using ImmersingLinker.Server.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ImmersingLinker.Test.Controllers;

public class AppControllerTest
{
    [Fact]
    public void Hello_ReturnsOkWithHelloWorld()
    {
        var controller = new AppController();
        var result = controller.Hello();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Hello world!", okResult.Value);
    }
}
