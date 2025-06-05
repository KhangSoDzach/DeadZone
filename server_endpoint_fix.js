// Add this code to your Node.js/Express server file

// Token verification endpoint
app.get('/api/auth/verify', auth, async (req, res) => {
  try {
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'User not found' });
    }
    
    // Return user data in the format expected by the game
    res.json({
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
    });
  } catch (error) {
    console.error('Token verification error:', error);
    res.status(500).json({ error: 'Server error' });
  }
});
