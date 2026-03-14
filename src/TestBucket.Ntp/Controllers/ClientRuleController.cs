using Microsoft.AspNetCore.Mvc;
using System.Net;
using TestBucket.Ntp.Core.Testing;

namespace TestBucket.Ntp.Controllers
{
    /// <summary>
    /// Manages NTP client rules that control the server's response behavior per client IP address.
    /// </summary>
    [ApiController]
    [Route("api/client-rules")]
    [Produces("application/json")]
    [Tags("Client Rules")]
    public class ClientRuleController : ControllerBase
    {
        private readonly IClientRuleRepository _repository;

        /// <summary>
        /// Initializes a new instance of <see cref="ClientRuleController"/>.
        /// </summary>
        /// <param name="repository">The client rule repository.</param>
        public ClientRuleController(IClientRuleRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Returns a paged list of all client rules.
        /// </summary>
        /// <param name="offset">The zero-based index of the first item to return.</param>
        /// <param name="count">The maximum number of items to return.</param>
        /// <returns>A list of <see cref="ClientRule"/> objects.</returns>
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<ClientRule>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClientRulesAsync([FromQuery] int offset = 0, [FromQuery] int count = 50)
        {
            try
            {
                var rules = await _repository.BrowseAsync(offset, count);
                return Ok(rules);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the client rule for the specified client IP address.
        /// </summary>
        /// <param name="clientIpAddress">The IP address of the client.</param>
        /// <returns>The <see cref="ClientRule"/> for the given IP address.</returns>
        [HttpGet("{clientIpAddress}")]
        [ProducesResponseType<ClientRule>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClientRuleAsync(string clientIpAddress)
        {
            if (!IPAddress.TryParse(clientIpAddress, out var ipAddress))
            {
                return Problem(detail: $"'{clientIpAddress}' is not a valid IP address.", statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                var rule = await _repository.GetClientRuleAsync(ipAddress);
                if (rule is null)
                {
                    return Problem(detail: $"No client rule found for IP address '{clientIpAddress}'.", statusCode: StatusCodes.Status404NotFound);
                }

                return Ok(rule);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Creates a new client rule.
        /// </summary>
        /// <param name="clientRule">The client rule to create.</param>
        /// <returns>The created <see cref="ClientRule"/>.</returns>
        [HttpPost]
        [ProducesResponseType<ClientRule>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateClientRuleAsync([FromBody] ClientRule clientRule)
        {
            try
            {
                await _repository.AddClientRuleAsync(clientRule);
                return CreatedAtAction(nameof(GetClientRuleAsync), new { clientIpAddress = clientRule.ClientIpAddress }, clientRule);
            }
            catch (ArgumentException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates an existing client rule. Creates a new rule if one does not already exist for the given IP address.
        /// </summary>
        /// <param name="clientIpAddress">The IP address of the client whose rule is being updated.</param>
        /// <param name="clientRule">The updated client rule.</param>
        /// <returns>The updated <see cref="ClientRule"/>.</returns>
        [HttpPut("{clientIpAddress}")]
        [ProducesResponseType<ClientRule>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateClientRuleAsync(string clientIpAddress, [FromBody] ClientRule clientRule)
        {
            if (!string.Equals(clientRule.ClientIpAddress, clientIpAddress, StringComparison.OrdinalIgnoreCase))
            {
                return Problem(
                    detail: "The ClientIpAddress in the request body must match the route parameter.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                await _repository.AddClientRuleAsync(clientRule);
                return Ok(clientRule);
            }
            catch (ArgumentException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deletes the client rule for the specified client IP address.
        /// </summary>
        /// <param name="clientIpAddress">The IP address of the client whose rule is to be deleted.</param>
        [HttpDelete("{clientIpAddress}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteClientRuleAsync(string clientIpAddress)
        {
            if (!IPAddress.TryParse(clientIpAddress, out var ipAddress))
            {
                return Problem(detail: $"'{clientIpAddress}' is not a valid IP address.", statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                var rule = await _repository.GetClientRuleAsync(ipAddress);
                if (rule is null)
                {
                    return Problem(detail: $"No client rule found for IP address '{clientIpAddress}'.", statusCode: StatusCodes.Status404NotFound);
                }

                await _repository.RemoveClientRuleAsync(rule);
                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
