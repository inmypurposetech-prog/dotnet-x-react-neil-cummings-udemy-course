using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseAPIController : ControllerBase
    {
        private IMediator? _mediator; // private properties are usually prefixed with an underscore
        // the protected item is available for the class we're declaring it in and also any other classes that derive from it
        // ?? checks for null and assigns if null
        protected IMediator Mediator => 
             _mediator ??= HttpContext.RequestServices.GetService<IMediator>() 
                ?? throw new InvalidOperationException("IMediator service is currently unavailable"); 
    }
}
