using System;
using Xunit;

namespace StampsService.Tests
{
    public class StampsClientTests
    {
        private String integrationId = Environment.GetEnvironmentVariable("STAMPS_INTEGRATION_ID");
        private String password = Environment.GetEnvironmentVariable("STAMPS_PASSWORD");
        private String username = Environment.GetEnvironmentVariable("STAMPS_USERNAME");

        private SwsimV84SoapClient client = new SwsimV84SoapClient(SwsimV84SoapClient.EndpointConfiguration.SwsimV84Soap);

        [Fact]
        public async void IntegrationDemo()
        {
            // build a Credentials object that can be used to authenticate every request
            var credentials = new Credentials();
            credentials.IntegrationID = Guid.Parse(integrationId);
            credentials.Username = username;
            credentials.Password = password;

            // use the Credentials object to request a new "Authenticator" token
            var authRequest = new AuthenticateUserRequest(credentials);
            var authResponse = await client.AuthenticateUserAsync(authRequest);
            var authToken = authResponse.Authenticator;

            // the Stamps API documentation recommends requesting an auth token and updating it:
            // https://developer.stamps.com/developer/docs/swsimv84.html#authentication

            // build a Rate object that can be used to describe the package and destination
            var rate = new RateV31();
            rate.FromZIPCode = "75001";
            rate.ToZIPCode = "75210";
            rate.ShipDate = DateTime.Now;

            var getRatesRequest = new GetRatesRequest(authToken, rate);
            var getRatesResponse = await client.GetRatesAsync(getRatesRequest);

            Console.WriteLine(getRatesResponse);


        }
    }
}
