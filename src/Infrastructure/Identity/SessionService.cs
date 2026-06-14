using Retailer.Application.Common.Caching;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Tokens.Identity;
using Retailer.Domain.Common.Enums;
using Retailer.Domain.Identity;
using Retailer.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Infrastructure.Identity;
public class SessionService : ISessionService
{
    private readonly IRepository<UserSession> _userSessionRepository;
    private readonly IStringLocalizer<SessionService> _t;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cache;
    private readonly ICacheKeyService _cacheKey;

    public SessionService(IRepository<UserSession> userSessions, IStringLocalizer<SessionService> stringLocalizer, ICurrentUser currentUser, ICacheService cache, ICacheKeyService cacheKey)
    {
        _userSessionRepository = userSessions;
        _t = stringLocalizer;
        _currentUser = currentUser;
        _cache = cache;
        _cacheKey = cacheKey;
    }

    public async Task<UserSession> GetUserSessionAsync(string token)
    {
        return await _cache.GetOrSetAsync(
            _cacheKey.GetCacheKey(nameof(UserSession), token),
            async () => await _userSessionRepository.GetAll()
                            .AsNoTracking()
                            .Where(us => us.Token == token)
                            .FirstOrDefaultAsync());
    }

    public async Task CreateSessionAsync(
            string userId,
            DateTime expiry,
            string token,
            string deviceId,
            string fcmToken,
            string appVersion,
            string deviceName,
            SessionType sessionType = SessionType.Normal,
            bool isRemember = true)
    {
        var session = new UserSession
        {
            ApplicationUserId = userId,
            Token = token,
            ExpiryDate = expiry,
            DeviceId = deviceId,
            Type = sessionType,
            IsRemember = isRemember,
            Version = appVersion,
            FCMToken = fcmToken,
            DeviceName = deviceName
        };

        await _userSessionRepository.AddAsync(session);
    }

    public async Task<bool> VerifyTokenSessionAsync(string token)
    {
        var now = DateTime.UtcNow;
        var session = await GetUserSessionAsync(token);

        if (session == null || session.ExpiryDate < now)
            return false;

        return true;
    }

    public async Task LogOutSessionAsync(string token)
    {
        token = token.Replace("Bearer", string.Empty).Trim();
        var session = await _userSessionRepository.GetAll().FirstOrDefaultAsync(x => x.Token == token);

        if (session == null)
            throw new NotFoundException(_t["Session not found"]);

        session.ExpiryDate = DateTime.UtcNow;
        await _userSessionRepository.UpdateAsync(session);
        await _cache.RemoveAsync(_cacheKey.GetCacheKey(nameof(UserSession), token));
    }

    public async Task LogOutAllSessionsAsync(string userId)
    {
        var sessions = await _userSessionRepository.GetAll()
                                          .Where(x => x.ApplicationUserId == userId && x.ExpiryDate > DateTime.UtcNow)
                                          .ToListAsync();

        foreach (var session in sessions)
        {
            session.ExpiryDate = DateTime.UtcNow;
            await _cache.RemoveAsync(_cacheKey.GetCacheKey(nameof(UserSession), session.Token));
        }

        await _userSessionRepository.UpdateRangeAsync(sessions);
    }

    public async Task LogOutAllSessionsExceptCurrentUserAsync(string userId)
    {
        var sessions = await _userSessionRepository.GetAll()
                                            .Where(x => (x.ApplicationUserId == userId) && (x.ExpiryDate > DateTime.UtcNow) && (x.Token != _currentUser.GetToken()))
                                            .ToListAsync();

        foreach (var session in sessions)
        {
            session.ExpiryDate = DateTime.UtcNow;
            await _cache.RemoveAsync(_cacheKey.GetCacheKey(nameof(UserSession), session.Token));
        }

        await _userSessionRepository.UpdateRangeAsync(sessions);
    }
}