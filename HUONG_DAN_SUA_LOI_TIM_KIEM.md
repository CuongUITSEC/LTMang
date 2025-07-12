# Hướng dẫn sửa lỗi tìm kiếm User trong Learnify

## Vấn đề hiện tại:

Không thể tìm kiếm được tài khoản của những người khác trên thanh tìm kiếm do **Firebase Database Rules** chặn quyền truy cập.

## Nguyên nhân:

Với rules hiện tại:

```json
"users": {
  "$uid": {
    ".read": "$uid === auth.uid",
    ".write": "$uid === auth.uid"
  }
}
```

User chỉ có thể đọc dữ liệu của **chính mình**, không thể đọc dữ liệu của user khác để tìm kiếm.

## Giải pháp:

### Bước 1: Cập nhật Firebase Database Rules

Vào Firebase Console > Realtime Database > Rules và thay thế bằng rules sau:

```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "publicUsers": {
      "$uid": {
        ".read": true,
        ".write": "$uid === auth.uid",
        ".validate": "newData.hasChildren(['username', 'avatarUrl']) && newData.child('username').isString() && newData.child('avatarUrl').isString()"
      }
    },
    "friendRequests": {
      "$receiverId": {
        ".read": "$receiverId === auth.uid",
        ".write": true
      }
    },
    "friends": {
      "$userId": {
        ".read": "$userId === auth.uid",
        ".write": "$userId === auth.uid"
      }
    },
    "friendsListChanges": {
      "$userId": {
        ".read": "$userId === auth.uid",
        ".write": "$userId === auth.uid"
      }
    },
    "friendAcceptedNotifications": {
      "$userId": {
        ".read": "$userId === auth.uid",
        ".write": true
      }
    }
  }
}
```

### Bước 2: Đồng bộ dữ liệu

1. **Chạy ứng dụng** - Current user sẽ tự động được đồng bộ sang `publicUsers`
2. **Đồng bộ tất cả users** (nếu cần):
   - Uncomment dòng này trong `MainViewModel.cs`:
   ```csharp
   var syncCount = await _firebaseService.SyncAllUsersToPublicAsync();
   ```
   - Chạy ứng dụng 1 lần để đồng bộ
   - Comment lại dòng đó để tránh chạy lại

### Bước 3: Test tìm kiếm

1. Chạy ứng dụng
2. Vào tab Friends
3. Tìm kiếm user khác bằng email hoặc username
4. Kết quả tìm kiếm sẽ hiển thị user khác

## Vấn đề Permission khi chấp nhận lời mời kết bạn

### Nguyên nhân:

Khi user A chấp nhận lời mời kết bạn từ user B, hệ thống cố gắng thêm bạn vào danh sách của cả hai user. Tuy nhiên, Firebase rules chỉ cho phép user viết vào danh sách bạn bè của chính mình, dẫn đến lỗi "Permission denied".

### Giải pháp đã áp dụng:

1. **Thay đổi logic AcceptFriendRequestAsync**: Chỉ thêm vào danh sách bạn bè của receiver (người chấp nhận), không thêm vào danh sách của sender.

2. **Sử dụng polling mechanism**: Sender sẽ tự động phát hiện thay đổi qua polling và tự thêm receiver vào danh sách bạn bè của mình.

3. **Thay đổi logic RemoveFriendAsync**: Chỉ xóa khỏi danh sách bạn bè của current user, user khác sẽ tự động xóa qua polling.

### Các thay đổi trong code:

#### FirebaseService.cs - AcceptFriendRequestAsync:

```csharp
// Trước: Thêm vào cả 2 danh sách bạn bè
var res2 = await _httpClient.PutAsync(urlFriendReceiver, contentReceiver);
var res3 = await _httpClient.PutAsync(urlFriendSender, contentSender);

// Sau: Chỉ thêm vào danh sách của receiver
var res2 = await _httpClient.PutAsync(urlFriendReceiver, contentReceiver);
// Sender sẽ tự động thêm qua polling
```

#### FirebaseService.cs - RemoveFriendAsync:

```csharp
// Trước: Xóa khỏi cả 2 danh sách bạn bè
var res1 = await _httpClient.DeleteAsync(url1);
var res2 = await _httpClient.DeleteAsync(url2);

// Sau: Chỉ xóa khỏi danh sách của current user
var res1 = await _httpClient.DeleteAsync(url1);
// User khác sẽ tự động xóa qua polling
```

### Test fix:

Chạy test method `TestPermissionFix()` trong `TestRealTimeFriendsUpdate.cs` để kiểm tra:

```csharp
var test = new TestRealTimeFriendsUpdate();
await test.TestPermissionFix();
```

### Kết quả mong đợi:

- ✅ Chấp nhận lời mời kết bạn thành công không còn lỗi permission
- ✅ Hủy kết bạn thành công không còn lỗi permission
- ✅ Danh sách bạn bè được đồng bộ real-time qua polling mechanism
- ✅ Thông báo thay đổi được gửi cho cả 2 user

### Lưu ý:

1. **Polling mechanism** đã được implement trong `MainViewModel.cs` và sẽ tự động đồng bộ danh sách bạn bè
2. **Notification system** sẽ thông báo thay đổi cho cả 2 user để trigger polling
3. **Debouncing** được áp dụng để tránh loop vô hạn khi polling
4. **Retry mechanism** được áp dụng cho notification để đảm bảo thông báo được gửi thành công

## Cấu trúc dữ liệu mới:

### `users` (private - chỉ owner đọc được):

```json
{
  "users": {
    "uid123": {
      "username": "user1",
      "email": "user1@example.com",
      "phoneNumber": "123456789",
      "avatarUrl": "/Images/avatar1.svg",
      "isOnline": true,
      "studyTime": {...},
      // ... dữ liệu riêng tư khác
    }
  }
}
```

### `publicUsers` (public - tất cả đều đọc được):

```json
{
  "publicUsers": {
    "uid123": {
      "username": "user1",
      "avatarUrl": "/Images/avatar1.svg",
      "email": "user1@example.com" // Có thể bỏ nếu muốn bảo mật hơn
    }
  }
}
```

## Các thay đổi code:

1. **FirebaseService.cs**:

   - `GetUserByUidAsync()` tìm trong `publicUsers` trước, sau đó `users`
   - `SyncUserToPublicAsync()` đồng bộ user sang public
   - `SyncAllUsersToPublicAsync()` đồng bộ tất cả users

2. **MainViewModel.cs**:

   - Đã xóa toàn bộ command, property, function liên quan đến TestSearchCommand và TestSearchFunctionAsync

3. **MainView.xaml**:
   - Đã xóa button "Test Search" khỏi UI

## Debug và kiểm tra:

- Xem **Debug Output** trong Visual Studio để theo dõi logs
- Kiểm tra Firebase Console > Realtime Database > Data để xem dữ liệu `publicUsers`
- Test với các UID thực tế từ Firebase

## Lưu ý bảo mật:

- `publicUsers` chỉ chứa thông tin cơ bản: username, avatar, (email tùy chọn)
- Dữ liệu nhạy cảm vẫn ở `users` và chỉ owner đọc được
- Có thể bỏ `email` khỏi `publicUsers` nếu muốn bảo mật hơn

# Hướng dẫn cải thiện tính năng cập nhật real-time danh sách bạn bè

## Tổng quan

Đã cải thiện tính năng kết bạn và hủy kết bạn để đảm bảo danh sách bạn bè được cập nhật ngay lập tức cho cả hai người dùng với cơ chế retry và polling nâng cao.

## Các thay đổi chính

### 1. Cải thiện FirebaseService

- **File**: `Learnify/Services/FirebaseService.cs`
- **Thay đổi**:
  - **MỚI**: Thêm `NotifyFriendsListChangeWithRetryAsync` với retry mechanism (3 lần thử)
  - Cải thiện `AcceptFriendRequestAsync` với thông báo song song cho cả 2 user
  - Cải thiện `RemoveFriendAsync` với thông báo song song cho cả 2 user
  - Thêm field `acceptedAt` vào dữ liệu bạn bè để tracking
  - Sử dụng `Task.WhenAll` để thông báo đồng thời cho cả 2 user

### 2. Cải thiện NotificationViewModel

- **File**: `Learnify/ViewModels/NotificationViewModel.cs`
- **Thay đổi**:
  - Sử dụng `ForceReloadFriendsListAsync` thay vì `ReloadFriendsListAsync`
  - Cải thiện logging và error handling
  - Đảm bảo reload danh sách bạn bè ngay lập tức sau khi chấp nhận

### 3. Cải thiện FriendInfoWindowViewModel

- **File**: `Learnify/ViewModels/FriendInfoWindowViewModel.cs`
- **Thay đổi**:
  - Sử dụng `ForceReloadFriendsListAsync` thay vì `ReloadFriendsListAsync`
  - Cải thiện logging và error handling
  - Đảm bảo reload danh sách bạn bè ngay lập tức sau khi hủy kết bạn

### 4. Cải thiện MainViewModel

- **File**: `Learnify/ViewModels/MainViewModel.cs`
- **Thay đổi**:
  - **MỚI**: Thêm `ForceReloadFriendsListAsync` để force reload và reset timestamp
  - Giảm polling interval từ 5 giây xuống 3 giây để phản hồi nhanh hơn
  - Cải thiện logging và error handling
  - Thêm StackTrace logging cho debugging

### 5. Test và Debug

- **File**: `TestRealTimeFriendsUpdate.cs`
- **Thay đổi**:
  - Tạo file test mới để kiểm tra tính năng cập nhật real-time
  - Test các scenario: chấp nhận lời mời, hủy kết bạn, polling mechanism
  - Test toàn bộ flow từ gửi lời mời đến chấp nhận và hủy kết bạn

## Cơ chế hoạt động

### 1. Khi chấp nhận lời mời kết bạn:

1. User B chấp nhận lời mời từ User A
2. Firebase cập nhật dữ liệu bạn bè cho cả hai
3. **MỚI**: Thông báo thay đổi song song cho cả hai user với retry mechanism
4. **MỚI**: Sử dụng `ForceReloadFriendsListAsync` để reload ngay lập tức
5. UI được cập nhật ngay lập tức cho cả hai người

### 2. Khi hủy kết bạn:

1. User A hoặc B hủy kết bạn
2. Firebase xóa dữ liệu bạn bè cho cả hai
3. **MỚI**: Thông báo thay đổi song song cho cả hai user với retry mechanism
4. **MỚI**: Sử dụng `ForceReloadFriendsListAsync` để reload ngay lập tức
5. UI được cập nhật ngay lập tức cho cả hai người

### 3. Polling mechanism cải tiến:

- **MỚI**: Kiểm tra thay đổi mỗi 3 giây (thay vì 5 giây)
- Sử dụng `HasFriendsListChangedAsync` để kiểm tra
- Tự động reload danh sách khi có thay đổi
- Dừng polling khi đóng ứng dụng

### 4. Retry mechanism:

- **MỚI**: Thử lại tối đa 3 lần khi thông báo thay đổi
- Exponential backoff: 1s, 2s, 3s
- Đảm bảo thông báo được gửi thành công

## Lợi ích

1. **Real-time updates nhanh hơn**: Danh sách bạn bè được cập nhật ngay lập tức
2. **Đồng bộ hai chiều**: Cả hai người dùng đều thấy thay đổi
3. **Reliability cao hơn**: Retry mechanism đảm bảo thông báo được gửi
4. **Performance tốt hơn**: Polling nhanh hơn (3s thay vì 5s)
5. **Error handling tốt hơn**: Logging chi tiết và StackTrace
6. **Resource management**: Tự động dừng polling khi đóng ứng dụng

## Testing

Sử dụng file `TestRealTimeFriendsUpdate.cs` để test các tính năng:

- Test chấp nhận lời mời kết bạn real-time
- Test hủy kết bạn real-time
- Test polling mechanism
- Test toàn bộ flow từ gửi lời mời đến chấp nhận và hủy kết bạn

## Lưu ý quan trọng

1. **Khi chấp nhận lời mời kết bạn**: Cả hai người sẽ thấy nhau trong danh sách bạn bè ngay lập tức
2. **Khi hủy kết bạn**: Cả hai người sẽ không thấy nhau trong danh sách bạn bè ngay lập tức
3. **Retry mechanism**: Đảm bảo thông báo được gửi ngay cả khi mạng chậm
4. **Force reload**: Đảm bảo UI được cập nhật ngay lập tức
5. **Polling nhanh hơn**: Phát hiện thay đổi nhanh hơn (3s thay vì 5s)
6. **Logging chi tiết**: Dễ dàng debug khi có vấn đề
7. **Error handling**: Xử lý lỗi một cách graceful
8. **Resource management**: Tự động dừng polling khi đóng ứng dụng
