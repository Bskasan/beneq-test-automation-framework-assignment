using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PactNet;
using Xunit;

public class ConsumerPactTests
{
    [Fact]
    public async Task Get_Status_Contract()
    {
        // Minimal contract example: describe the expected interaction
        var pact = Pact.V3("Dashboard", "EquipmentApi", new PactConfig());
        await pact.UponReceiving("get status")
                  .WithRequest(HttpMethod.Get, "/api/status")
                  .WillRespond()
                  .WithStatus(HttpStatusCode.OK)
                  .WithJsonBody(new { state = "Running", speed = 100 });
    }
}
