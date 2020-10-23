using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Web3.Accounts;
using System.Numerics;
using System.Threading.Tasks;

namespace Nethereum.GoogleFunction
{

    public class Web3ConnectionOptions
    {
        public const string ConfigurationSection = "Web3Connection";
        public string Url { get; set; }
    }

    /// <summary>
    /// This needs to be secured!
    /// </summary>
    public class EthereumAccountOptions
    {
        public const string ConfigurationSection = "EthereumAccount";
        public string PrivateKey { get; set; }
    }

    /// <summary>
    /// A service connecting to a database, configured using <see cref="Web3ConnectionOptions"/>.
    /// </summary>
    public class Web3Service
    {
        public Web3.Web3 Web3 { get; }

        public Web3Service(Web3ConnectionOptions web3Options, EthereumAccountOptions accountOptions = null)
        {
            if(accountOptions == null || string.IsNullOrEmpty(accountOptions.PrivateKey))
            {
                Web3 = new Web3.Web3(web3Options.Url);
            }
            else
            {
                Web3 = new Web3.Web3(new Account(accountOptions.PrivateKey), web3Options.Url);
            }
        }      
    }


    /// <summary>
    /// The startup class is provided with a host builder which exposes a service collection
    /// and configuration. This can be used to make additional dependencies available.
    /// In this case, we configure a <see cref="Web3Service"/> based on the configuration.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            // Bind the connection options based on the current configuration.
            var web3Options = new Web3ConnectionOptions();
            context.Configuration.GetSection(Web3ConnectionOptions.ConfigurationSection)
                .Bind(web3Options);

            var accountOptions = new EthereumAccountOptions();
            context.Configuration
                .GetSection(EthereumAccountOptions.ConfigurationSection)
                .Bind(accountOptions);

            // Build the web3 service from the connection options.
            var web3Service = new Web3Service(web3Options, accountOptions);

            // Add the web3 service to the service collection.
            services.AddSingleton(web3Service);
        }
    }


    [FunctionsStartup(typeof(Startup))]
    public class Function : IHttpFunction
    {
        private readonly Web3Service _web3Service;

        public Function(Web3Service web3Service) =>
            _web3Service = web3Service;


        /// <summary>
        /// Logic for your function goes here.
        /// </summary>
        /// <param name="context">The HTTP context, containing the request and the response.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task HandleAsync(HttpContext context)
        {
            var web3 = _web3Service.Web3;

            // Check the balance of one of the accounts provisioned in our chain, to do that, 
            // we can execute the GetBalance request asynchronously:
            var balance = await web3.Eth.GetBalance.SendRequestAsync("0xde0b295669a9fd93d5f28d9ec85e40f4cb697bae");
            
            await context.Response.WriteAsync("Balance Ether: " + Nethereum.Web3.Web3.Convert.FromWei(balance.Value).ToString());

            var balanceOfMessage = new BalanceOfFunction() { Owner = "0x8ee7d9235e01e6b42345120b5d270bdb763624c7" };

            //Creating a new query handler
            var queryHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();

            //Querying the Maker smart contract https://etherscan.io/address/0x9f8f72aa9304c8b593d555f12ef6589cc3a579a2
            var balanceSmartContract = await queryHandler
                .QueryAsync<BigInteger>("0x9f8f72aa9304c8b593d555f12ef6589cc3a579a2", balanceOfMessage)
                .ConfigureAwait(false);

            await context.Response.WriteAsync(" Balance Smart contract: " + Nethereum.Web3.Web3.Convert.FromWei(balanceSmartContract).ToString());
  
        }

        [Function("balanceOf", "uint256")]
        public class BalanceOfFunction : FunctionMessage
        {
            [Parameter("address", "_owner", 1)] public string Owner { get; set; }
        }
    }
}
