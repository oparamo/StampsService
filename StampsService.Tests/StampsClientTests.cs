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

            // the Stamps API documentation recommends requesting an auth token first then updating with each subsequent request:
            // https://developer.stamps.com/developer/docs/swsimv84.html#authentication

            // build a Rate object that can be used to describe the package and destination
            var rateQuery = new RateV31();
            rateQuery.FromZIPCode = "75201";
            rateQuery.ToZIPCode = "75207";
            rateQuery.ShipDate = DateTime.Now;
            rateQuery.PackageType = PackageTypeV9.Package;
            rateQuery.WeightLb = 12;

            // get rates based off the given rate object
            var getRatesRequest = new GetRatesRequest(authToken, rateQuery);
            var getRatesResponse = await client.GetRatesAsync(getRatesRequest);

            // pick a rate to create indicium later and update the authtoken
            // a rate is chosen here just for demo purposes
            var rate = getRatesResponse.Rates[0];
            // the rate objects contain all possible add-ons, some of them are not compatible with each other
            // for that reason, set the rate add-ons to be a single item array to avoid any issues in this demo
            rate.AddOns = Array.FindAll(rate.AddOns, addOn => addOn.AddOnType == AddOnTypeV15.USADC);
            authToken = getRatesResponse.Authenticator;

            // create and cleanse a "from" address
            var fromAddress = new Address();
            fromAddress.FullName = "John Doe";
            fromAddress.Address1 = "2200 Ross Ave";
            fromAddress.City = "Dallas";
            fromAddress.State = "TX";
            fromAddress.PostalCode = "75201";
            fromAddress.PhoneNumber = "9728590000";

            var cleanseFromAddressRequest = new CleanseAddressRequest();
            cleanseFromAddressRequest.Item = authToken;
            cleanseFromAddressRequest.Address = fromAddress;

            var cleanseFromAddressResponse = await client.CleanseAddressAsync(cleanseFromAddressRequest);
            // get the cleansed fromAddress in the response and update the auth token
            fromAddress = cleanseFromAddressResponse.Address;
            authToken = cleanseFromAddressResponse.Authenticator;

            // create and cleanse a "to" address
            var toAddress = new Address();
            toAddress.FullName = "Jane Doe";
            toAddress.Address1 = "300 Reunion Boulevard";
            toAddress.City = "Dallas";
            toAddress.State = "TX";
            toAddress.PostalCode = "75207";
            toAddress.EmailAddress = "oscar@paramo.dev";

            var cleanseToAddressRequest = new CleanseAddressRequest();
            cleanseToAddressRequest.Item = authToken;
            cleanseToAddressRequest.Address = toAddress;

            var cleanseToAddressResponse = await client.CleanseAddressAsync(cleanseToAddressRequest);
            // get the cleansed toAddress in the response and update the auth token
            toAddress = cleanseToAddressResponse.Address;
            authToken = cleanseToAddressResponse.Authenticator;

            // create indicium with the given rate and address
            var createIndiciumRequest = new CreateIndiciumRequest();
            createIndiciumRequest.Item = authToken;
            // in a real application, this should be a client generated unique ID for the transaction
            // if a request fails, this ID could be reused to retry the request without risk of duplicate billing
            createIndiciumRequest.IntegratorTxID = "demo-transaction-id";
            createIndiciumRequest.Rate = rate;
            createIndiciumRequest.From = fromAddress;
            createIndiciumRequest.To = toAddress;
            // setting SampleOnly to true (false by default) allows creating testing labels in the testing environment
            createIndiciumRequest.SampleOnly = true;

            // if this request wasn't a SampleOnly, you may need to ensure your account has a postage balance to pay for the shipping label
            var createIndiciumResponse = await client.CreateIndiciumAsync(createIndiciumRequest);

            // get some important shipping details from the createIndiciumResponse and update the auth token
            var trackingNumber = createIndiciumResponse.TrackingNumber;
            var url = createIndiciumResponse.URL;
            var postageBalance = createIndiciumResponse.PostageBalance;
            var stampsTxID = createIndiciumResponse.StampsTxID;
            authToken = createIndiciumResponse.Authenticator;

            // print some of the previous details
            Console.WriteLine($"Tracking number: {trackingNumber}");
            Console.WriteLine($"Postage URL: {url}");
            Console.WriteLine($"Available postage balance: {postageBalance.AvailablePostage}");
            Console.WriteLine($"Stamps transaction ID: {stampsTxID}");

            // create a track shipment object, either trackingNumber or stampsTxID could be used here
            //var trackShipmentRequest = new TrackShipmentRequest(authToken, stampsTxID);
            var trackShipmentRequest = new TrackShipmentRequest(authToken, trackingNumber);
            // unfortunately this request doesn't work with tracking numbers from test labels
            var trackShipmentResponse = await client.TrackShipmentAsync(trackShipmentRequest);
            var trackingEvents = trackShipmentResponse.TrackingEvents;

            // the auth token could also be updated here so it's used in future requests
            // but that isn't needed for this demo since we generate a new auth token in the beginning

            Console.WriteLine($"Tracking service description: {trackShipmentResponse.ServiceDescription}");
            foreach (var trackingEvent in trackingEvents)
            {
                Console.WriteLine($"Event description: {trackingEvent.Event}");
                Console.WriteLine($"Event type: {trackingEvent.TrackingEventType}");
            }
        }
    }
}
