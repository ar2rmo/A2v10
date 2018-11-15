﻿// Copyright © 2015-2018 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNet.Identity;

using A2v10.Data.Interfaces;
using A2v10.Infrastructure;
using System.Configuration;

namespace A2v10.Web.Identity
{
	public class AppUserStore :
		IDisposable,
		IUserStore<AppUser, Int64>,
		IUserLoginStore<AppUser, Int64>,
		IUserLockoutStore<AppUser, Int64>,
		IUserPasswordStore<AppUser, Int64>,
		IUserTwoFactorStore<AppUser, Int64>,
		IUserEmailStore<AppUser, Int64>,
		IUserPhoneNumberStore<AppUser, Int64>,
		IUserSecurityStampStore<AppUser, Int64>,
		IUserRoleStore<AppUser, Int64>,
		IUserClaimStore<AppUser, Int64>
	{

		class UserCache
		{
			Dictionary<String, AppUser> _mapNames = new Dictionary<String, AppUser>();
			Dictionary<Int64, AppUser> _mapIds = new Dictionary<Int64, AppUser>();

			public AppUser GetById(Int64 id)
			{
				if (_mapIds.TryGetValue(id, out AppUser user))
					return user;
				return null;
			}
			public AppUser GetByName(String name)
			{
				if (_mapNames.TryGetValue(name, out AppUser user))
					return user;
				return null;
			}

			public AppUser GetByEmail(String email)
			{
				foreach (var u in _mapIds)
					if (u.Value.Email == email)
						return u.Value;
				return null;
			}

			public AppUser GetByPhoneNumber(String phone)
			{
				foreach (var u in _mapIds)
					if (u.Value.PhoneNumber == phone)
						return u.Value;
				return null;
			}

			public void CacheUser(AppUser user)
			{
				if (user == null)
					return;
				if (!_mapIds.ContainsKey(user.Id))
				{
					_mapIds.Add(user.Id, user);
				}
				else
				{
					var existing = _mapIds[user.Id];
					if (!Comparer<AppUser>.Equals(user, existing))
						throw new InvalidProgramException("Invalid user cache");
				}
				if (!_mapNames.ContainsKey(user.UserName))
				{
					_mapNames.Add(user.UserName, user);
				}
				else
				{
					var existing = _mapIds[user.Id];
					if (!Comparer<AppUser>.Equals(user, existing))
						throw new InvalidProgramException("Invalid user cache");
				}
			}
		}

		private readonly IDbContext _dbContext;
		private readonly IApplicationHost _host;

		private UserCache _cache;

		private String _customSchema;

		public AppUserStore(IDbContext dbContext, IApplicationHost host)
		{
			_dbContext = dbContext;
			_host = host;
			_cache = new UserCache();
		}

		public String DbSchema => _customSchema ?? "a2security";

		public void SetCustomSchema(String schema)
		{
			_customSchema = schema;
		}

		internal String DataSource => _host.CatalogDataSource;

		#region IUserStore

		public async Task CreateAsync(AppUser user)
		{
			await _dbContext.ExecuteAsync(DataSource, $"[{DbSchema}].[CreateUser]", user);
			if (_host.IsMultiTenant)
			{
				/*
				var createdUser = await FindByIdAsync(user.Id);
				_host.TenantId = createdUser.Tenant;
				await _dbContext.ExecuteAsync(_host.TenantDataSource, $"[{DbSchema}].[CreateTenantUser]", createdUser);
				CacheUser(createdUser);
				*/
			}
			else
			{
				CacheUser(user);
			}
		}

		public Task DeleteAsync(AppUser user)
		{
			throw new NotImplementedException();
		}

		public async Task<AppUser> FindByIdAsync(Int64 userId)
		{
			AppUser user = _cache.GetById(userId);
			if (user != null)
				return user;
			user = await _dbContext.LoadAsync<AppUser>(DataSource, $"[{DbSchema}].[FindUserById]", new { Id = userId });
			CacheUser(user);
			return user;
		}

		public async Task<AppUser> FindByNameAsync(String userName)
		{
			AppUser user = _cache.GetByName(userName);
			if (user != null)
				return user;
			user = await _dbContext.LoadAsync<AppUser>(DataSource, $"[{DbSchema}].[FindUserByName]", new { UserName = userName });
			CacheUser(user);
			return user;
		}

		public async Task UpdateAsync(AppUser user)
		{
			if (user.IsPhoneNumberModified)
			{
				// verify Phone number
				await _dbContext.ExecuteAsync<AppUser>(DataSource, $"[{DbSchema}].[ConfirmPhoneNumber]", user);
				user.ClearModified(UserModifiedFlag.PhoneNumber | UserModifiedFlag.LastLogin | UserModifiedFlag.Password);
			}
			if (user.IsLockoutModified)
			{
				await _dbContext.ExecuteAsync<AppUser>(DataSource, $"[{DbSchema}].[UpdateUserLockout]", user);
				// do not call last login here
				user.ClearModified(UserModifiedFlag.Lockout | UserModifiedFlag.LastLogin);
			}
			if (user.IsPasswordModified)
			{
				await _dbContext.ExecuteAsync<AppUser>(DataSource, $"[{DbSchema}].[UpdateUserPassword]", user);
				user.ClearModified(UserModifiedFlag.Password);
			}
			if (user.IsLastLoginModified)
			{
				await _dbContext.ExecuteAsync<AppUser>(DataSource, $"[{DbSchema}].[UpdateUserLogin]", user);
				user.ClearModified(UserModifiedFlag.LastLogin);

			}
			if (user.IsEmailConfirmModified)
			{
				await _dbContext.ExecuteAsync<AppUser>(DataSource, $"[{DbSchema}].[ConfirmEmail]", user);
				user.ClearModified(UserModifiedFlag.EmailConfirmed);
				if (user.EmailConfirmed)
				{
					await CreateTenantUser(user);
				}
			}
		}

		#endregion

		async Task CreateTenantUser(AppUser user)
		{
			if (_host.IsMultiTenant)
			{
				var createdUser = await FindByIdAsync(user.Id);
				_host.TenantId = createdUser.Tenant;
				await _dbContext.ExecuteAsync(_host.TenantDataSource, $"[{DbSchema}].[CreateTenantUser]", createdUser);
			}
		}

		void CacheUser(AppUser user)
		{
			_cache.CacheUser(user);
		}

		#region IDisposable Support
		private Boolean disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(Boolean disposing)
		{
			if (!disposedValue)
			{
				_cache = null;
				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			Dispose(true);
		}
		#endregion

		#region IUserLoginStore
		public async Task AddLoginAsync(AppUser user, UserLoginInfo login)
		{
			await _dbContext.ExecuteAsync(DataSource, $"[{DbSchema}].[AddUserLogin]", new { UserId = user.Id, login.LoginProvider, login.ProviderKey });
		}

		public Task RemoveLoginAsync(AppUser user, UserLoginInfo login)
		{
			throw new NotImplementedException();
		}

		public Task<IList<UserLoginInfo>> GetLoginsAsync(AppUser user)
		{
			IList<UserLoginInfo> list = new List<UserLoginInfo>();
			return Task.FromResult(list);
		}

		public async Task<AppUser> FindAsync(UserLoginInfo login)
		{
			if (login.LoginProvider == "PhoneNumber")
				return await FindByPhoneNumberAsync(login.ProviderKey);
			var user = await _dbContext.LoadAsync<AppUser>(DataSource, $"[{DbSchema}].[FindUserByLogin]", new { login.LoginProvider, login.ProviderKey });
			return user;
		}
		#endregion

		#region IUserLockoutStore
		public Task<DateTimeOffset> GetLockoutEndDateAsync(AppUser user)
		{
			return Task.FromResult<DateTimeOffset>(user.LockoutEndDateUtc);
		}

		public Task SetLockoutEndDateAsync(AppUser user, DateTimeOffset lockoutEnd)
		{
			if (user.LockoutEndDateUtc != lockoutEnd)
			{
				user.LockoutEndDateUtc = lockoutEnd;
				user.SetModified(UserModifiedFlag.Lockout);
			}
			return Task.FromResult<Int32>(0);
		}

		public Task<Int32> IncrementAccessFailedCountAsync(AppUser user)
		{
			user.AccessFailedCount += 1;
			user.SetModified(UserModifiedFlag.Lockout);
			return Task.FromResult(user.AccessFailedCount);
		}

		public Task ResetAccessFailedCountAsync(AppUser user)
		{
			if (user.AccessFailedCount != 0)
			{
				user.AccessFailedCount = 0;
				user.SetModified(UserModifiedFlag.Lockout);
			}
			return Task.FromResult(0);
		}

		public Task<Int32> GetAccessFailedCountAsync(AppUser user)
		{
			return Task.FromResult(user.AccessFailedCount);
		}

		public Task<Boolean> GetLockoutEnabledAsync(AppUser user)
		{
			return Task.FromResult(user.LockoutEnabled);
		}

		public Task SetLockoutEnabledAsync(AppUser user, Boolean enabled)
		{
			user.LockoutEnabled = enabled;
			return Task.FromResult(0);
		}
		#endregion

		#region IUserPasswordStore
		public Task SetPasswordHashAsync(AppUser user, String passwordHash)
		{
			user.PasswordHash = passwordHash;
			user.SetModified(UserModifiedFlag.Password);
			return Task.FromResult(0);
		}

		public Task<String> GetPasswordHashAsync(AppUser user)
		{
			return Task.FromResult(user.PasswordHash);
		}

		public Task<Boolean> HasPasswordAsync(AppUser user)
		{
			return Task.FromResult(user.PasswordHash != null);

		}
		#endregion

		#region IUserTwoFactorStore
		public Task SetTwoFactorEnabledAsync(AppUser user, Boolean enabled)
		{
			user.TwoFactorEnabled = enabled;
			return Task.FromResult(0);
		}

		public Task<Boolean> GetTwoFactorEnabledAsync(AppUser user)
		{
			return Task.FromResult(user.TwoFactorEnabled);
		}
		#endregion

		#region IUserEmailStore
		public Task SetEmailAsync(AppUser user, String email)
		{
			user.Email = email;
			return Task.FromResult(0);
		}

		public Task<String> GetEmailAsync(AppUser user)
		{
			String mail = user.Email;
			if (String.IsNullOrEmpty(mail))
				mail = user.UserName; // user name as email
			return Task.FromResult(mail);
		}

		public Task<Boolean> GetEmailConfirmedAsync(AppUser user)
		{
			return Task.FromResult(user.EmailConfirmed);
		}

		public Task SetEmailConfirmedAsync(AppUser user, Boolean confirmed)
		{
			user.EmailConfirmed = confirmed;
			user.SetModified(UserModifiedFlag.EmailConfirmed);
			return Task.FromResult(0);
		}

		public async Task<AppUser> FindByEmailAsync(String email)
		{
			AppUser user = _cache.GetByEmail(email);
			if (user != null)
				return user;
			user = await _dbContext.LoadAsync<AppUser>(DataSource, $"[{DbSchema}].[FindUserByEmail]", new { Email = email });
			CacheUser(user);
			return user;
		}

		#endregion

		public async Task<AppUser> FindByPhoneNumberAsync(String phone)
		{
			AppUser user = _cache.GetByPhoneNumber(phone);
			if (user != null)
				return user;
			user = await _dbContext.LoadAsync<AppUser>(DataSource, $"[{DbSchema}].[FindUserByPhoneNumber]", new { PhoneNumber = phone });
			CacheUser(user);
			return user;
		}


		#region IUserPhoneNumberStore
		public Task SetPhoneNumberAsync(AppUser user, String phoneNumber)
		{
			if (user.PhoneNumber != phoneNumber)
			{
				user.PhoneNumber = phoneNumber;
				user.SetModified(UserModifiedFlag.PhoneNumber);
			}
			return Task.FromResult(0);
		}

		public Task<String> GetPhoneNumberAsync(AppUser user)
		{
			return Task.FromResult(user.PhoneNumber);
		}

		public Task<Boolean> GetPhoneNumberConfirmedAsync(AppUser user)
		{
			return Task.FromResult(user.PhoneNumberConfirmed);
		}

		public Task SetPhoneNumberConfirmedAsync(AppUser user, Boolean confirmed)
		{
			if (user.PhoneNumberConfirmed != confirmed)
			{
				user.PhoneNumberConfirmed = confirmed;
				user.SetModified(UserModifiedFlag.PhoneNumber);
			}
			return Task.FromResult(0);
		}
		#endregion

		#region IUserSecurityStampStore
		public Task SetSecurityStampAsync(AppUser user, String stamp)
		{
			if (user.SecurityStamp != stamp)
			{
				user.SecurityStamp = stamp;
				user.SetModified(UserModifiedFlag.Password);
			}
			return Task.FromResult(0);
		}

		public Task<String> GetSecurityStampAsync(AppUser user)
		{
			return Task.FromResult(user.SecurityStamp);
		}
		#endregion


		async Task AddAppClaims(AppUser user, List<Claim> list, String claims)
		{
			foreach (var s in claims.Split(','))
			{
				var claim = s.Trim().ToLowerInvariant();
				switch (claim)
				{
					case "groups":
						await AddGroupsToClaims(user, list);
						break;
				}
			}
		}

		async Task AddGroupsToClaims(AppUser user, List<Claim> claims)
		{
			var groups = await _dbContext.LoadListAsync<AppRole>(DataSource, $"[{DbSchema}].[GetUserGroups]", new { UserId = user.Id });
			var glist = groups.Where(role => role.Key != null && role.Key != "Users").Select(role => role.Key.ToLowerInvariant());
			String gstr = String.Join(",", glist);
			claims.Add(new Claim("groups", gstr));
		}

		#region IUserClaimStore 
		public async Task<IList<Claim>> GetClaimsAsync(AppUser user)
		{
			//TODO:
			/* добавляем все элементы, которые могут быть нужны БЕЗ загрузки объекта 
             * доступ через 
             * var user = HttpContext.Current.User.Identity as ClaimsIdentity;
             */
			List<Claim> list = new List<Claim>
			{
				new Claim("PersonName", user.PersonName ?? String.Empty),
				new Claim("TenantId", user.Tenant.ToString())
			};
			if (user.IsAdmin)
				list.Add(new Claim("Admin", "Admin"));

			/*
			list.Add(new Claim("Locale", user.Locale ?? "uk_UA"));
			list.Add(new Claim("AppKey", user.ComputedAppKey));
			*/

			// there is not http context!
			var claims = ConfigurationManager.AppSettings["useClaims"];
			if (!String.IsNullOrEmpty(claims))
				await AddAppClaims(user, list, claims);
			return list;
		}

		public Task AddClaimAsync(AppUser user, Claim claim)
		{
			throw new NotImplementedException("AddClaimAsync");
		}

		public Task RemoveClaimAsync(AppUser user, Claim claim)
		{
			throw new NotImplementedException("RemoveClaimAsync");
		}
		#endregion

		#region IUserRoleStore

		IList<String> _userRoles = null;

		public Task AddToRoleAsync(AppUser user, String roleName)
		{
			_userRoles = null;
			throw new NotImplementedException();
		}

		public Task RemoveFromRoleAsync(AppUser user, String roleName)
		{
			_userRoles = null;
			throw new NotImplementedException();
		}

		public async Task<IList<String>> GetRolesAsync(AppUser user)
		{
			if (_userRoles != null)
				return _userRoles;
			var list = await _dbContext.LoadListAsync<AppRole>(DataSource, $"[{DbSchema}].[GetUserGroups]", new { UserId = user.Id });
			_userRoles =  list.Select<AppRole, String>(x => x.Name).ToList();
			return _userRoles;
		}

		public async Task<Boolean> IsInRoleAsync(AppUser user, String roleName)
		{
			IList<String> roles = await GetRolesAsync(user);
			return roles.IndexOf(roleName) != -1;
		}
		#endregion
	}
}
