//using System;
//using System.Collections.Generic;
//using System.Text;
using GoBuddy.BusinessLayer.DTOs;
using System.Threading.Tasks;

namespace GoBuddy.BusinessLayer.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequestDTO request);

        Task<string> LoginAsync(LoginRequestDTO request);
    }
}
