using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System.Numerics;
using System.Threading.Tasks;

namespace Nethereum.GoogleFunction
{
    public class Function : IHttpFunction
    {
        /// <summary>
        /// Logic for your function goes here.
        /// </summary>
        /// <param name="context">The HTTP context, containing the request and the response.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task HandleAsync(HttpContext context)
        {
            var web3 = new Nethereum.Web3.Web3("https://mainnet.infura.io/v3/7238211010344719ad14a89db874158c");

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
