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
2. Click nút **"Test Search"** để test tự động
3. Hoặc nhập UID trong thanh tìm kiếm thủ công
4. Kiểm tra Debug Output để xem logs

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
