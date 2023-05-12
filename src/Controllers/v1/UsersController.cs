using Asp.Versioning;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers.v1;

[ApiVersion(1.0)]
public class UsersController : BaseODataController
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _user;
    private readonly ILogger<UsersController> _logger;
    private readonly IMapper _mapper;

    public UsersController(
        ILogger<UsersController> logger,
        IUserService userService,
        ICurrentUserService user,
        ApplicationDbContext context,
        IMapper mapper)
    {
        _logger = logger;
        _userService = userService;
        _user = user;
        _context = context;
        _mapper = mapper;
    }

    // GET: /api/v1/users
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), Status200OK)]
    [EnableQuery(PageSize = 1_000)]
    [RoleAuthorizeAttribute(Role.Employee, Role.Administrator)]
    public ActionResult Get()
    {
        return Ok(_userService.GetAllUsers());
    }

    // GET: /api/v1/users/1
    [HttpGet]
    [ProducesResponseType(typeof(UserDto), Status200OK)]
    [ProducesResponseType(typeof(NotFoundResult), Status404NotFound)]
    [EnableQuery]
    [RoleAuthorizeAttribute(Role.Employee, Role.Administrator)]
    public ActionResult<UserDto> Get(Guid key)
    {
        var user = _userService.GetAllUsers()
            .FirstOrDefault(x => x.Id == key);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    // POST: /api/v1/users
    [HttpPost]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(typeof(UserDto), Status201Created)]
    [ProducesResponseType(Status400BadRequest)]
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.None)]
    [RoleAuthorizeAttribute(Role.Administrator)]
    public async Task<ActionResult<UserDto>> Post([FromBody] CreateUserRequest request, [FromServices] IValidator<CreateUserRequest> validator)
    {
        var result = validator.Validate(request);
        if (!result.IsValid)
        {
            result.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var user = new User
        {
            Email = request.Email,
            Firstname = request.FirstName,
            Lastname = request.LastName,
            AvatarUrl = request.AvatarUrl,
            Role = Role.Customer,
            Status = UserStatus.Active
        };

        if (_user.Role is Role.Administrator)
        {
            user.Role = request.Role;
        }

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = user.Id }, _mapper.Map<UserDto>(user));
    }

    // PUT: /api/v1/users/1
    [HttpPut]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(typeof(UserDto), Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.None)]
    [RoleAuthorizeAttribute(Role.Administrator)]
    public async Task<ActionResult<UserDto>> Put([FromRoute] Guid key, [FromBody] UpdateUserRequest request, [FromServices] IValidator<UpdateUserRequest> validator)
    {
        var result = validator.Validate(request);
        if (!result.IsValid)
        {
            result.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var user = _context.Users.Find(key);

        if (user is null)
        {
            return NotFound("User not found");
        }

        user.Email = request.Email;
        user.Firstname = request.FirstName;
        user.Lastname = request.LastName;
        user.Status = request.Status;

        if (_user.Role is Role.Administrator)
        {
            user.Role = request.Role;
        }

        await _context.SaveChangesAsync();

        return Updated(_mapper.Map<UserDto>(user));
    }

    // DELETE: /api/v1/products/1
    [HttpDelete]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(typeof(NoContentResult), Status204NoContent)]
    [ProducesResponseType(Status404NotFound)]
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.None)]
    [RoleAuthorizeAttribute(Role.Administrator)]
    public async Task<ActionResult> Delete([FromRoute] Guid key)
    {
        var product = _context.Users.Where(x => !x.IsDeleted)
            .FirstOrDefault(x => x.Id == key);

        if (product is null)
        {
            return NotFound("Product not found");
        }

        product.IsDeleted = true;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}