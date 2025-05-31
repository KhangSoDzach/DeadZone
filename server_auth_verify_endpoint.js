// Add this endpoint to your Node.js/Express server

// Import required modules (adjust based on your server setup)
// const jwt = require('jsonwebtoken');
// const User = require('./models/User'); // Adjust path to your User model

// Auth middleware (if not already defined)
const auth = (req, res, next) => {
  try {
    const token = req.header('x-auth-token');
    if (!token) {
      return res.status(401).json({ error: 'Không có token xác thực' });
    }
    
    const decoded = jwt.verify(token, 'your_jwt_secret_key'); // Use your actual secret
    req.userId = decoded.userId;
    next();
  } catch (error) {
    res.status(401).json({ error: 'Token không hợp lệ' });
  }
};

// Helper function to format user response consistently
function formatUserResponse(user) {
  return {
    id: user._id.toString(), // Convert MongoDB ObjectId to string
    username: user.username,
    email: user.email || '',
    level: user.level || 1,
    experience: user.experience || 0,
    money: user.money || 0,
    health: user.health || 100,
    kills: user.kills || 0,
    lastLoginDate: user.lastLoginDate || new Date().toISOString(),
    checkpoint: user.checkpoint || null,
    weapons: user.weapons || []
  };
}

// ADD THIS ENDPOINT TO YOUR SERVER
app.get('/api/auth/verify', auth, async (req, res) => {
  try {
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'User not found' });
    }
    
    // Return user data in the format expected by the game
    res.json(formatUserResponse(user));
  } catch (error) {
    console.error('Token verification error:', error);
    res.status(500).json({ error: 'Server error' });
  }
});

// Also update your existing /api/player/data endpoint to use the same format
app.get('/api/player/data', auth, async (req, res) => {
  try {
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'User not found' });
    }
    
    // Use the same formatting function for consistency
    res.json(formatUserResponse(user));
  } catch (error) {
    console.error('Player data fetch error:', error);
    res.status(500).json({ error: 'Server error' });
  }
});

// Update login endpoint to use consistent formatting
app.post('/api/auth/login', async (req, res) => {
  try {
    const { username, password } = req.body;
    
    // Your existing login validation logic here...
    // const user = await User.findOne({ username });
    // const isMatch = await bcrypt.compare(password, user.password);
    // if (!isMatch) return res.status(400).json({ error: 'Invalid credentials' });
    
    // Generate token
    const token = jwt.sign({ userId: user._id }, 'your_jwt_secret_key');
    
    // Return consistent response format
    res.json({
      token,
      user: formatUserResponse(user)
    });
  } catch (error) {
    console.error('Login error:', error);
    res.status(500).json({ error: 'Internal server error' });
  }
});
