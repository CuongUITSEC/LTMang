using System;
using System.Threading.Tasks;
using Learnify.Services;

class TestFixFriends
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing Fix Missing Friends Data...");
        
        // Bạn cần set token để test (lấy từ app thực)
        // AuthService.SetToken("your_token_here");
        
        var firebaseService = new FirebaseService();
        var fixedCount = await firebaseService.FixMissingFriendsDataAsync();
        
        Console.WriteLine($"Fixed {fixedCount} missing friends relationships");
        Console.WriteLine("Test completed. Press any key to exit...");
        Console.ReadKey();
    }
}
