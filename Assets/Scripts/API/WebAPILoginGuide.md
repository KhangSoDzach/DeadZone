# Hướng dẫn cài đặt hệ thống đăng nhập/đăng ký WebAPI

## Tổng quan
Hệ thống đăng nhập/đăng ký này được thiết kế để kết nối game Dead Zone với backend API sử dụng MongoDB và NodeJS. Hệ thống cho phép người dùng đăng nhập, đăng ký tài khoản và đồng bộ dữ liệu giữa game và server.

## Cấu trúc thư mục và tệp

### 1. Các tệp chính
- **LoginManager.cs**: Quản lý đăng nhập/đăng ký và xác thực người dùng
- **UserDataModel.cs**: Model dữ liệu người dùng 
- **UserDataSync.cs**: Đồng bộ dữ liệu người dùng giữa client và server
- **LoginUIManager.cs**: Quản lý UI đăng nhập và đăng ký

## Hướng dẫn thiết lập

### 1. Thiết lập Scene đăng nhập
1. Tạo một Scene đăng nhập mới (hoặc sử dụng Scene hiện có)
2. Tạo Canvas chứa UI đăng nhập/đăng ký với các panel sau:
   - Welcome Panel (màn hình chào)
   - Login Panel (màn hình đăng nhập)
   - Register Panel (màn hình đăng ký)
   - Loading Panel (màn hình loading)
   - Error Panel (màn hình hiển thị lỗi)

3. Thêm các UI elements vào mỗi panel:
   - **Welcome Panel**: Các nút Login, Register, Play Offline, và thông tin phiên bản
   - **Login Panel**: Input fields cho username và password, nút Submit và Back, toggle Remember Me
   - **Register Panel**: Input fields cho username, email, password và confirm password, toggle Terms of Service, nút Submit và Back
   - **Loading Panel**: Thanh tiến trình loading và text hiển thị trạng thái
   - **Error Panel**: Text hiển thị lỗi và nút Close

4. Thêm script `LoginUIManager.cs` vào Canvas và kéo-thả các tham chiếu UI vào các trường tương ứng

5. Tạo một GameObject trống có tên "API" và thêm script `LoginManager.cs` vào đó

### 2. Cài đặt WebAPI (Backend)

Đảm bảo WebAPI của bạn có các endpoint sau:
- `POST /api/auth/login`: Đăng nhập với username và password
- `POST /api/auth/register`: Đăng ký người dùng mới
- `GET /api/player/data`: Lấy dữ liệu người dùng
- `POST /api/player/data`: Lưu dữ liệu người dùng

Ví dụ cấu trúc backend NodeJS:
```javascript
const express = require('express');
const mongoose = require('mongoose');
const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');

// Kết nối MongoDB
mongoose.connect('mongodb://localhost:27017/deadzone', { 
  useNewUrlParser: true, 
  useUnifiedTopology: true 
});

// Định nghĩa Schema người dùng
const UserSchema = new mongoose.Schema({
  username: { type: String, required: true, unique: true },
  email: { type: String, required: true, unique: true },
  password: { type: String, required: true },
  created: { type: Date, default: Date.now },
  playerData: {
    level: { type: Number, default: 1 },
    experience: { type: Number, default: 0 },
    money: { type: Number, default: 0 },
    // Các thông tin khác...
  }
});

// Tạo model
const User = mongoose.model('User', UserSchema);

// Thiết lập Express
const app = express();
app.use(express.json());

// Endpoint đăng ký
app.post('/api/auth/register', async (req, res) => {
  try {
    const { username, email, password } = req.body;
    
    // Kiểm tra user đã tồn tại
    const existingUser = await User.findOne({ $or: [{ username }, { email }] });
    if (existingUser) {
      return res.status(400).json({ error: 'Username hoặc email đã tồn tại' });
    }
    
    // Hash password
    const salt = await bcrypt.genSalt(10);
    const hashedPassword = await bcrypt.hash(password, salt);
    
    // Tạo người dùng mới
    const user = new User({
      username,
      email,
      password: hashedPassword
    });
    
    await user.save();
    
    // Tạo token
    const token = jwt.sign({ userId: user._id }, 'your_jwt_secret_key', { expiresIn: '7d' });
    
    res.json({ token });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Lỗi server' });
  }
});

// Endpoint đăng nhập
app.post('/api/auth/login', async (req, res) => {
  try {
    const { username, password } = req.body;
    
    // Tìm user
    const user = await User.findOne({ username });
    if (!user) {
      return res.status(400).json({ error: 'Tên đăng nhập không đúng' });
    }
    
    // Kiểm tra password
    const isMatch = await bcrypt.compare(password, user.password);
    if (!isMatch) {
      return res.status(400).json({ error: 'Mật khẩu không đúng' });
    }
    
    // Tạo token
    const token = jwt.sign({ userId: user._id }, 'your_jwt_secret_key', { expiresIn: '7d' });
    
    res.json({ token });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Lỗi server' });
  }
});

// Middleware kiểm tra xác thực
const auth = async (req, res, next) => {
  try {
    const token = req.header('x-auth-token');
    if (!token) {
      return res.status(401).json({ error: 'Không có token xác thực' });
    }
    
    const decoded = jwt.verify(token, 'your_jwt_secret_key');
    req.userId = decoded.userId;
    next();
  } catch (error) {
    res.status(401).json({ error: 'Token không hợp lệ' });
  }
};

// Endpoint lấy dữ liệu người chơi
app.get('/api/player/data', auth, async (req, res) => {
  try {
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'Người dùng không tồn tại' });
    }
    
    res.json({
      username: user.username,
      email: user.email,
      level: user.playerData.level,
      experience: user.playerData.experience,
      money: user.playerData.money
      // Thêm các thông tin khác...
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Lỗi server' });
  }
});

// Endpoint lưu dữ liệu người chơi
app.post('/api/player/data', auth, async (req, res) => {
  try {
    const { level, experience, money } = req.body;
    
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'Người dùng không tồn tại' });
    }
    
    // Cập nhật dữ liệu
    user.playerData.level = level || user.playerData.level;
    user.playerData.experience = experience || user.playerData.experience;
    user.playerData.money = money || user.playerData.money;
    
    await user.save();
    
    res.json({ success: true });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Lỗi server' });
  }
});

// Khởi động server
app.listen(5000, () => {
  console.log('Server đang chạy trên cổng 5000');
});
```

### 3. Tích hợp với game

1. Đặt URL API đúng trong `GameAPI.cs`:
```csharp
private const string API_URL = "http://your-api-server:5000/api";
```

2. Thêm GameObject "DataSync" vào MainScene và gắn script `UserDataSync.cs` vào đó

3. Điều chỉnh các scene name trong `LoginManager.cs` và `LoginUIManager.cs` cho phù hợp với cấu trúc game của bạn

## Tùy chỉnh hệ thống

### 1. Mở rộng UserDataModel
Bạn có thể mở rộng `UserDataModel.cs` để thêm các trường dữ liệu cần thiết cho game của bạn, ví dụ:
- Danh sách vũ khí đã mở khóa
- Tiến độ các nhiệm vụ
- Thống kê chiến đấu
- v.v.

### 2. Tùy chỉnh giao diện
Bạn có thể tùy chỉnh giao diện đăng nhập/đăng ký để phù hợp với phong cách của game:
- Thay đổi màu sắc, font chữ
- Thêm hình ảnh và hiệu ứng
- Điều chỉnh layout

### 3. Thêm tính năng xã hội
Bạn có thể mở rộng hệ thống để thêm các tính năng xã hội như:
- Đăng nhập bằng Google/Facebook
- Xếp hạng người chơi
- Chia sẻ thành tích

## Xử lý lỗi và bảo mật

1. Luôn kiểm tra kết nối internet trước khi gọi API
2. Xử lý các trường hợp token hết hạn và tự động đăng nhập lại
3. Lưu trữ mật khẩu an toàn (không lưu trực tiếp, chỉ lưu token)
4. Thêm encryption cho dữ liệu người dùng nhạy cảm

## Hỗ trợ chế độ offline
Nếu muốn hỗ trợ chơi offline, bạn cần:
1. Lưu dữ liệu người dùng cục bộ trong PlayerPrefs hoặc tệp JSON
2. Đồng bộ dữ liệu khi có kết nối internet
3. Xử lý xung đột dữ liệu giữa phiên bản cục bộ và server
