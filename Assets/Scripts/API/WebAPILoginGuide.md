# Hướng dẫn cài đặt hệ thống đăng nhập/đăng ký WebAPI

## Tổng quan
Hệ thống đăng nhập/đăng ký này được thiết kế để kết nối game Dead Zone với backend API sử dụng MongoDB và NodeJS. Hệ thống cho phép người dùng đăng nhập, đăng ký tài khoản và đồng bộ dữ liệu giữa game và server.

**Lưu ý quan trọng về cấu trúc dữ liệu**: Hệ thống đã được cập nhật để xử lý các vấn đề về đồng bộ user identity và đảm bảo tính nhất quán dữ liệu giữa client và server.

## Cấu trúc thư mục và tệp

### 1. Các tệp chính
- **GameAPI.cs**: Core API handler với improved user data validation
- **LoginManager.cs**: Quản lý đăng nhập/đăng ký và xác thực người dùng
- **UserDataModel.cs**: Model dữ liệu người dùng 
- **UserDataSync.cs**: Đồng bộ dữ liệu người dùng giữa client và server với user identity protection
- **LoginUIManager.cs**: Quản lý UI đăng nhập và đăng ký

## Cải tiến mới

### 1. Bảo vệ User Identity
- Tự động khôi phục thông tin user (username, ID, email) nếu bị mất trong quá trình sync
- Multiple fallback endpoints để lấy dữ liệu user
- Validation cải tiến cho dữ liệu user

### 2. Error Handling nâng cao
- Retry logic cho các API calls
- Alternative endpoints khi primary endpoint fails
- Better error messages cho user

### 3. Data Consistency
- Đảm bảo user identity được preserved trong mọi trường hợp
- Automatic data initialization cho missing fields
- Improved sync logic với backup/restore functionality

### 4. Các tệp chính
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

**Lưu ý quan trọng**: Đảm bảo server của bạn có các endpoint chính xác sau:

**Endpoint cần thiết:**
- `POST /api/auth/login`: Đăng nhập với username và password
- `POST /api/auth/register`: Đăng ký người dùng mới
- `GET /api/auth/verify`: Xác thực token (với header x-auth-token)
- `GET /api/player/data`: Lấy dữ liệu người dùng
- `PUT /api/player/save` **HOẶC** `PUT /api/player/data`: Lưu dữ liệu người dùng

Ví dụ server NodeJS đã sửa lỗi MongoDB connection:
```javascript
const express = require('express');
const mongoose = require('mongoose');
const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const cors = require('cors');

// Enhanced MongoDB connection with better error handling
const connectDB = async () => {
  try {
    // Add more connection options for stability
    const conn = await mongoose.connect('mongodb://localhost:27017/deadzone', {
      useNewUrlParser: true,
      useUnifiedTopology: true,
      serverSelectionTimeoutMS: 5000, // Timeout after 5s instead of 30s
      socketTimeoutMS: 45000, // Close sockets after 45s of inactivity
      family: 4 // Use IPv4, skip trying IPv6
    });

    console.log(`MongoDB Connected: ${conn.connection.host}`);
    
    // Test the connection
    await mongoose.connection.db.admin().ping();
    console.log('MongoDB ping successful');
    
    return true;
  } catch (error) {
    console.error('MongoDB connection error:', error);
    
    // Try to connect to alternative MongoDB instances
    console.log('Trying alternative connection...');
    try {
      const altConn = await mongoose.connect('mongodb://127.0.0.1:27017/deadzone', {
        useNewUrlParser: true,
        useUnifiedTopology: true,
        serverSelectionTimeoutMS: 5000
      });
      console.log(`MongoDB Connected (alternative): ${altConn.connection.host}`);
      return true;
    } catch (altError) {
      console.error('Alternative MongoDB connection also failed:', altError);
      return false;
    }
  }
};

// Enhanced User Schema with validation
const UserSchema = new mongoose.Schema({
  username: { 
    type: String, 
    required: [true, 'Username is required'], 
    unique: true,
    trim: true,
    minlength: [3, 'Username must be at least 3 characters'],
    maxlength: [30, 'Username cannot exceed 30 characters']
  },
  email: { 
    type: String, 
    required: [true, 'Email is required'], 
    unique: true,
    trim: true,
    lowercase: true,
    match: [/^\w+([.-]?\w+)*@\w+([.-]?\w+)*(\.\w{2,3})+$/, 'Please enter a valid email']
  },
  password: { 
    type: String, 
    required: [true, 'Password is required'],
    minlength: [6, 'Password must be at least 6 characters']
  },
  created: { type: Date, default: Date.now },
  level: { type: Number, default: 1, min: 1 },
  experience: { type: Number, default: 0, min: 0 },
  money: { type: Number, default: 0, min: 0 },
  health: { type: Number, default: 100, min: 0, max: 100 },
  lastLoginDate: { type: String, default: '' },
  checkpoint: {
    sceneId: { type: String, default: '' },
    position: {
      x: { type: Number, default: 0 },
      y: { type: Number, default: 0 },
      z: { type: Number, default: 0 }
    },
    timestamp: { type: String, default: '' },
    additionalData: { type: String, default: '' }
  },
  weapons: [{
    id: String,
    name: String,
    damage: { type: Number, default: 0 },
    level: { type: Number, default: 1 },
    isUnlocked: { type: Boolean, default: false },
    ammo: { type: Number, default: 0 }
  }]
});

// Add indexes for better performance
UserSchema.index({ username: 1 });
UserSchema.index({ email: 1 });

const User = mongoose.model('User', UserSchema);

// Enhanced Express setup
const app = express();
app.use(cors());
app.use(express.json({ limit: '10mb' })); // Increase payload limit

// Add request logging middleware
app.use((req, res, next) => {
  console.log(`${new Date().toISOString()} - ${req.method} ${req.path}`);
  next();
});

// Health check endpoint
app.get('/', async (req, res) => {
  try {
    // Check MongoDB connection
    const mongoStatus = mongoose.connection.readyState;
    const mongoStatusText = ['disconnected', 'connected', 'connecting', 'disconnecting'][mongoStatus];
    
    // Test database operation
    let dbTest = false;
    try {
      await mongoose.connection.db.admin().ping();
      dbTest = true;
    } catch (dbError) {
      console.error('Database ping failed:', dbError);
    }
    
    res.json({ 
      message: 'Dead Zone API Server is running',
      mongodb: {
        status: mongoStatusText,
        connected: mongoStatus === 1,
        ping: dbTest
      },
      timestamp: new Date().toISOString()
    });
  } catch (error) {
    console.error('Health check error:', error);
    res.status(500).json({ 
      error: 'Server health check failed',
      details: error.message 
    });
  }
});

// Enhanced registration endpoint with better error handling
app.post('/api/auth/register', async (req, res) => {
  console.log('Registration attempt:', req.body.username);
  
  try {
    const { username, email, password } = req.body;
    
    // Validate input
    if (!username || !email || !password) {
      console.log('Registration failed: Missing required fields');
      return res.status(400).json({ error: 'Username, email and password are required' });
    }
    
    // Check MongoDB connection before proceeding
    if (mongoose.connection.readyState !== 1) {
      console.error('MongoDB not connected, current state:', mongoose.connection.readyState);
      return res.status(500).json({ error: 'Database connection error. Please try again later.' });
    }
    
    // Test database connectivity
    try {
      await mongoose.connection.db.admin().ping();
    } catch (pingError) {
      console.error('Database ping failed during registration:', pingError);
      return res.status(500).json({ error: 'Database connectivity issue. Please try again.' });
    }
    
    // Check if user already exists
    console.log('Checking for existing user...');
    const existingUser = await User.findOne({ $or: [{ username }, { email }] });
    if (existingUser) {
      console.log('Registration failed: User already exists');
      if (existingUser.username === username) {
        return res.status(400).json({ error: 'Username already exists' });
      } else {
        return res.status(400).json({ error: 'Email already exists' });
      }
    }
    
    console.log('Hashing password...');
    // Hash password
    const salt = await bcrypt.genSalt(10);
    const hashedPassword = await bcrypt.hash(password, salt);
    
    console.log('Creating new user...');
    // Create new user with explicit field mapping
    const newUser = new User({
      username: username.trim(),
      email: email.trim().toLowerCase(),
      password: hashedPassword,
      level: 1,
      experience: 0,
      money: 0,
      health: 100,
      lastLoginDate: new Date().toISOString().slice(0, 19).replace('T', ' '),
      checkpoint: {
        sceneId: '',
        position: { x: 0, y: 0, z: 0 },
        timestamp: '',
        additionalData: ''
      },
      weapons: []
    });
    
    console.log('Saving user to database...');
    // Save with error handling
    let savedUser;
    try {
      savedUser = await newUser.save();
      console.log('User saved successfully:', savedUser._id);
    } catch (saveError) {
      console.error('Error saving user to database:', saveError);
      
      // Handle specific MongoDB errors
      if (saveError.code === 11000) {
        // Duplicate key error
        const duplicateField = Object.keys(saveError.keyPattern)[0];
        return res.status(400).json({ error: `${duplicateField} already exists` });
      }
      
      return res.status(500).json({ 
        error: 'Failed to create user account',
        details: saveError.message 
      });
    }
    
    console.log('Generating JWT token...');
    // Generate token
    const token = jwt.sign({ userId: savedUser._id }, 'your_jwt_secret_key', { expiresIn: '7d' });
    
    // Prepare response data
    const responseData = {
      token,
      user: {
        id: savedUser._id.toString(),
        username: savedUser.username,
        email: savedUser.email,
        level: savedUser.level || 1,
        experience: savedUser.experience || 0,
        money: savedUser.money || 0,
        health: savedUser.health || 100,
        lastLoginDate: savedUser.lastLoginDate,
        checkpoint: savedUser.checkpoint || null,
        weapons: savedUser.weapons || []
      }
    };
    
    console.log('Registration successful for:', savedUser.username);
    console.log('Response data:', JSON.stringify(responseData, null, 2));
    
    res.json(responseData);
    
  } catch (error) {
    console.error('Registration error:', error);
    
    // Differentiate between different types of errors
    if (error.name === 'ValidationError') {
      const validationErrors = Object.values(error.errors).map(err => err.message);
      return res.status(400).json({ 
        error: 'Validation failed',
        details: validationErrors.join(', ')
      });
    }
    
    if (error.name === 'MongoError' || error.name === 'MongoServerError') {
      console.error('MongoDB specific error:', error);
      return res.status(500).json({ error: 'Database error. Please try again later.' });
    }
    
    res.status(500).json({ 
      error: 'Internal server error during registration',
      details: error.message 
    });
  }
});

// Enhanced login endpoint
app.post('/api/auth/login', async (req, res) => {
  try {
    const { username, password } = req.body;
    
    console.log(`Login attempt for user: ${username}`);
    
    // Validate input
    if (!username || !password) {
      console.log('Login failed: Missing username or password');
      return res.status(400).json({ error: 'Username and password are required' });
    }
    
    // Check MongoDB connection
    if (mongoose.connection.readyState !== 1) {
      console.error('MongoDB not connected during login, current state:', mongoose.connection.readyState);
      return res.status(500).json({ error: 'Database connection error. Please try again later.' });
    }
    
    // Find user with error handling
    let user;
    try {
      user = await User.findOne({ username });
    } catch (findError) {
      console.error('Error finding user:', findError);
      return res.status(500).json({ error: 'Database query error. Please try again.' });
    }
    
    if (!user) {
      console.log(`User not found: ${username}`);
      return res.status(400).json({ error: 'Invalid username or password' });
    }
    
    // Check password
    const isMatch = await bcrypt.compare(password, user.password);
    if (!isMatch) {
      console.log(`Invalid password for user: ${username}`);
      return res.status(400).json({ error: 'Invalid username or password' });
    }
    
    // Update last login with error handling
    try {
      user.lastLoginDate = new Date().toISOString().slice(0, 19).replace('T', ' ');
      await user.save();
    } catch (updateError) {
      console.error('Error updating last login date:', updateError);
      // Continue with login even if this fails
    }
    
    // Generate token
    const token = jwt.sign({ userId: user._id }, 'your_jwt_secret_key', { expiresIn: '7d' });
    
    console.log(`Login successful for user: ${username}, ID: ${user._id}`);
    
    // Prepare user data
    const userData = {
      id: user._id.toString(),
      username: user.username,
      email: user.email,
      level: user.level || 1,
      experience: user.experience || 0,
      money: user.money || 0,
      health: user.health || 100,
      lastLoginDate: user.lastLoginDate,
      checkpoint: user.checkpoint || null,
      weapons: user.weapons || []
    };
    
    const responseData = { 
      token,
      user: userData
    };
    
    console.log('Login response data:', JSON.stringify(responseData, null, 2));
    
    // Validate response before sending
    if (!responseData.token || !responseData.user || !responseData.user.username) {
      console.error('Response validation failed:', responseData);
      return res.status(500).json({ error: 'Server error: Invalid response data' });
    }
    
    res.json(responseData);
  } catch (error) {
    console.error('Login error:', error);
    res.status(500).json({ 
      error: 'Internal server error during login',
      details: error.message 
    });
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

// Endpoint xác thực token
app.get('/api/auth/verify', auth, async (req, res) => {
  try {
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'Người dùng không tồn tại' });
    }
    
    res.json({
      id: user._id,
      username: user.username,
      email: user.email,
      level: user.level,
      experience: user.experience,
      money: user.money,
      health: user.health,
      lastLoginDate: user.lastLoginDate,
      checkpoint: user.checkpoint,
      weapons: user.weapons || []
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Lỗi server' });
  }
});

// Endpoint lấy dữ liệu người chơi
app.get('/api/player/data', auth, async (req, res) => {
  try {
    console.log(`Fetching player data for user ID: ${req.userId}`);
    
    const user = await User.findById(req.userId);
    if (!user) {
      console.log(`User not found with ID: ${req.userId}`);
      return res.status(404).json({ error: 'Người dùng không tồn tại' });
    }
    
    // Return complete user data - ENSURE ALL FIELDS ARE INCLUDED
    const userData = {
      id: user._id.toString(), // Convert ObjectId to string
      username: user.username,
      email: user.email,
      level: user.level || 1,
      experience: user.experience || 0,
      money: user.money || 0,
      health: user.health || 100,
      lastLoginDate: user.lastLoginDate,
      checkpoint: user.checkpoint || null,
      weapons: user.weapons || []
    };
    
    console.log('Player data response:', JSON.stringify(userData, null, 2));
    res.json(userData);
  } catch (error) {
    console.error('Get player data error:', error);
    res.status(500).json({ error: 'Lỗi server' });
  }
});

// Endpoint lưu dữ liệu người chơi - CHÍNH XÁC!
app.put('/api/player/save', auth, async (req, res) => {
  try {
    const updateData = req.body;
    
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'Người dùng không tồn tại' });
    }
    
    // Cập nhật dữ liệu
    if (updateData.level !== undefined) user.level = updateData.level;
    if (updateData.experience !== undefined) user.experience = updateData.experience;
    if (updateData.money !== undefined) user.money = updateData.money;
    if (updateData.health !== undefined) user.health = updateData.health;
    if (updateData.lastLoginDate !== undefined) user.lastLoginDate = updateData.lastLoginDate;
    if (updateData.checkpoint !== undefined) user.checkpoint = updateData.checkpoint;
    if (updateData.weapons !== undefined) user.weapons = updateData.weapons;
    
    await user.save();
    
    res.json({ 
      success: true,
      message: 'Dữ liệu đã được lưu thành công'
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Lỗi server khi lưu dữ liệu' });
  }
});

// Alternative endpoint for saving (fallback)
app.put('/api/player/data', auth, async (req, res) => {
  try {
    const updateData = req.body;
    
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'Người dùng không tồn tại' });
    }
    
    // Cập nhật dữ liệu
    if (updateData.level !== undefined) user.level = updateData.level;
    if (updateData.experience !== undefined) user.experience = updateData.experience;
    if (updateData.money !== undefined) user.money = updateData.money;
    if (updateData.health !== undefined) user.health = updateData.health;
    if (updateData.lastLoginDate !== undefined) user.lastLoginDate = updateData.lastLoginDate;
    if (updateData.checkpoint !== undefined) user.checkpoint = updateData.checkpoint;
    if (updateData.weapons !== undefined) user.weapons = updateData.weapons;
    
    await user.save();
    
    res.json({ 
      success: true,
      message: 'Dữ liệu đã được lưu thành công'
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Lỗi server khi lưu dữ liệu' });
  }
});

// Khởi động server
const PORT = process.env.PORT || 5000;

const startServer = async () => {
  console.log('Starting Dead Zone API Server...');
  
  // Connect to MongoDB first
  const dbConnected = await connectDB();
  
  if (!dbConnected) {
    console.error('Failed to connect to MongoDB. Server will start but database operations will fail.');
    console.error('Please ensure MongoDB is running on localhost:27017');
  }
  
  // Start the server regardless of DB connection
  app.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
    console.log('Available endpoints:');
    console.log('- GET / (health check)');
    console.log('- POST /api/auth/login');
    console.log('- POST /api/auth/register');
    console.log('- GET /api/auth/verify');
    console.log('- GET /api/player/data');
    console.log('- PUT /api/player/save');
    console.log('- PUT /api/player/data');
    
    if (dbConnected) {
      console.log('✓ MongoDB connected successfully');
    } else {
      console.log('✗ MongoDB connection failed - check database server');
    }
  });
};

// Handle unhandled promise rejections
process.on('unhandledRejection', (err, promise) => {
  console.error('Unhandled rejection at:', promise, 'reason:', err);
});

// Handle MongoDB connection errors
mongoose.connection.on('error', (err) => {
  console.error('MongoDB connection error:', err);
});

mongoose.connection.on('disconnected', () => {
  console.warn('MongoDB disconnected');
});

// Start the server
startServer();
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

## Debug và khắc phục sự cố

### Lỗi "Unable to access user profile"
Lỗi này thường xảy ra khi:
1. **Server response thiếu user data**: Kiểm tra server có trả về đầy đủ thông tin user không
2. **API endpoint không đúng**: Verify các endpoints `/api/player/data` và `/api/auth/verify` hoạt động
3. **Token không hợp lệ**: Kiểm tra token có được gửi đúng trong header không

**Cách khắc phục:**
```csharp
// Test trong Unity Console
StartCoroutine(GameAPI.Instance.TestServerConnection((success, message) => {
    Debug.Log($"Server test: {success} - {message}");
}));

// Test get player data
StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
    Debug.Log($"Get player data: {success} - {error}");
    if (success) {
        Debug.Log($"User: {GameAPI.Instance.PlayerData?.username}");
    }
}));
```

### Lỗi 404 Token Verification (Mới)
**Triệu chứng**: `[GameAPI] Token verification response code: 404`

**Nguyên nhân**: Server không có endpoint `/api/auth/verify`

**Giải pháp**:
1. **Thêm endpoint vào server**: Đảm bảo server có endpoint `/api/auth/verify`
2. **Sử dụng fallback**: GameAPI đã được cập nhật để tự động fallback sang `/api/player/data`
3. **Kiểm tra server**: Verify server đang chạy và có đúng endpoints

**Kiểm tra server endpoints**:
```bash
# Test endpoints bằng curl hoặc Postman
curl -X GET http://localhost:5000/api/auth/verify -H "x-auth-token: YOUR_TOKEN"
curl -X GET http://localhost:5000/api/player/data -H "x-auth-token: YOUR_TOKEN"
```

**Cập nhật server nếu thiếu endpoint**:
```javascript
// Thêm vào server.js nếu chưa có
app.get('/api/auth/verify', auth, async (req, res) => {
  try {
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'Người dùng không tồn tại' });
    }
    
    res.json({
      id: user._id.toString(),
      username: user.username,
      email: user.email,
      level: user.level,
      experience: user.experience,
      money: user.money,
      health: user.health,
      lastLoginDate: user.lastLoginDate,
      checkpoint: user.checkpoint,
      weapons: user.weapons || []
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Lỗi server' });
  }
});
```

### Lỗi 404 Not Found
1. Kiểm tra server có đang chạy không
2. Kiểm tra URL trong `GameAPI.cs` có đúng không
3. Kiểm tra endpoints trong server có khớp với client không
4. Sử dụng Postman để test các endpoints trực tiếp

### Lỗi Authentication
1. Kiểm tra username/password có đúng không
2. Verify server hash password đúng cách
3. Kiểm tra JWT token có được tạo và gửi đúng không

### Test server connectivity
Thêm method test trong GameAPI:
```csharp
// Test in Unity Console
StartCoroutine(GameAPI.Instance.TestServerConnection((success, message) => {
    Debug.Log($"Server test: {success} - {message}");
}));
```

### Xử lý khi server offline
Hệ thống đã được cập nhật để:
- Không hiển thị lỗi 404 khi khởi động nếu server offline
- Tự động fallback sang endpoint khác nếu endpoint chính không khả dụng
- Chỉ hiển thị lỗi quan trọng cho người dùng
