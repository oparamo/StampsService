using Xunit;

namespace StampsService.Tests
{
    public class StampsClientTests
    {
        [Fact]
        public void IntegrationTest()
        {
            var credentials = new Credentials
            {
                IntegrationID = "dc2cc1f4-7797-4d59-9fa6-bd057006ea67";
                Username = "OPSC-001";
                Password = "October2019!";
            };

            SwsimV84SoapClient client = new SwsimV84SoapClient(SwsimV84SoapClient.EndpointConfiguration.SwsimV84Soap12);

            var authRequest = new AuthenticateUserRequest(credentials);
            var authResponse = await client.AuthenticateUserAsync(authRequest);
        }
    }
}
