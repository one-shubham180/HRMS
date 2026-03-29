using HRMS.Application.Features.Recruitment.Commands;
using HRMS.Application.Features.Recruitment.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[Authorize]
public class RecruitmentController : ApiControllerBase
{
    [Authorize(Roles = "Admin,HR")]
    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCandidatesQuery(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("candidates")]
    public async Task<IActionResult> CreateCandidate([FromBody] CreateCandidateCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("candidates/{candidateId:guid}/status")]
    public async Task<IActionResult> UpdateCandidateStatus(Guid candidateId, [FromBody] UpdateCandidateStatusCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { CandidateId = candidateId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("appraisals")]
    public async Task<IActionResult> GetAppraisals([FromQuery] Guid? employeeId = null, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetPerformanceAppraisalsQuery(employeeId), cancellationToken);
        return Ok(result);
    }
}
