using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _user;
    private readonly IMapper _mapper;

    public UserService(ApplicationDbContext context, ICurrentUserService user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public IQueryable<UserDto> GetAllUsers()
    {
        if (_user.Role is Role.Administrator or Role.Employee)
        {
            return _context.Users.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider);
        }

        return _context.Users.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Id == _user.Id)
            .ProjectTo<UserDto>(_mapper.ConfigurationProvider);
    }
}