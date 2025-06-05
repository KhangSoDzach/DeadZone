// Add this code to your Node.js/Express server file

// This function ensures that the user data matches the structure expected by the client
function formatUserResponse(user) {
  // Make sure we convert MongoDB _id to id for the client
  return {
    id: user._id.toString(), // Convert ObjectId to string and ensure field name is 'id'
    username: user.username,  // Ensure username property exists
    email: user.email || '',
    level: user.level || 1,
    experience: user.experience || 0,
    money: user.money || 0,
    health: user.health || 100,
    lastLoginDate: user.lastLoginDate || new Date().toISOString(),
    checkpoint: user.checkpoint || null,
    weapons: user.weapons || []
  };
}

// Modified login endpoint
app.post('/api/auth/login', async (req, res) => {
  try {
    const { username, password } = req.body;
    
    // ... existing validation and user lookup logic ...
    
    // Use formatUserResponse when sending the response
    const token = jwt.sign({ userId: user._id }, 'your_jwt_secret_key');
    const userData = formatUserResponse(user);
    
    res.json({
      token,
      user: userData
    });
  } catch (error) {
    console.error('Login error:', error);
    res.status(500).json({ error: 'Internal server error' });
  }
});

// Modified player data endpoint
app.get('/api/player/data', auth, async (req, res) => {
  try {
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'User not found' });
    }
    
    // Use formatUserResponse to return consistent user data structure
    res.json(formatUserResponse(user));
  } catch (error) {
    console.error('Player data fetch error:', error);
    res.status(500).json({ error: 'Server error' });
  }
});

// Modified token verification endpoint
app.get('/api/auth/verify', auth, async (req, res) => {
  try {
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'User not found' });
    }
    
    // Use formatUserResponse to return consistent user data structure
    res.json(formatUserResponse(user));
  } catch (error) {
    console.error('Token verification error:', error);
    res.status(500).json({ error: 'Server error' });
  }
});
