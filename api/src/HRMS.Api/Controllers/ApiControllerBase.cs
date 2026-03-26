using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ISender Sender => HttpContext.RequestServices.GetRequiredService<ISender>();
}
