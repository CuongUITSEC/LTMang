using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace Learnify.Services
{
    public static class AuthService
    {
        private static string _currentToken;
        private static string _currentUserId;
        private static string _currentUsername;

        public static void SetToken(string token)
        {
            _currentToken = token;
        }

        public static string GetToken()
        {
            return _currentToken;
        }

        public static void SetUserId(string userId)
        {
            _currentUserId = userId;
        }

        public static void SetUsername(string username)
        {
            _currentUsername = username;
        }

        public static string GetUsername()
        {
            return _currentUsername;
        }

        public static string GetUserId()
        {
            return _currentUserId;
        }

        public static void ClearToken()
        {
            _currentToken = null;
            _currentUserId = null;
            _currentUsername = null;
        }

        public static bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(_currentToken) && !string.IsNullOrEmpty(_currentUserId);
        }
    }
} 