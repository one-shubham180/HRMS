using HRMS.Application.Features.Documents.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[Authorize]
public class DocumentsController : ApiControllerBase
{
    [HttpGet("{employeeId:guid}")]
    public async Task<IActionResult> GetEmployeeVault(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetEmployeeDocumentsQuery(employeeId), cancellationToken);
        return Ok(result);
    }
}
