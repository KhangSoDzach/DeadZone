// Enhanced server endpoints to ensure proper data overwriting
// Add this to your Node.js/Express server file

// Middleware to log all save requests for debugging
app.use('/api/player/save', (req, res, next) => {
    console.log('=== SAVE REQUEST DEBUG ===');
    console.log('Method:', req.method);
    console.log('Headers:', req.headers);
    console.log('Body preview:', JSON.stringify(req.body).substring(0, 200) + '...');
    console.log('User ID from token:', req.userId);
    console.log('==========================');
    next();
});

// Enhanced primary save endpoint with explicit overwrite semantics
app.put('/api/player/save', auth, async (req, res) => {
    try {
        const userId = req.userId;
        const saveData = req.body;
        
        // Validate required fields
        if (!saveData.userId || !saveData.username) {
            return res.status(400).json({ error: 'Missing required user identification fields' });
        }
        
        // Ensure the save data belongs to the authenticated user
        if (saveData.userId !== userId) {
            return res.status(403).json({ error: 'User ID mismatch - cannot save data for different user' });
        }
        
        console.log(`Processing save for user: ${saveData.username} (ID: ${userId})`);
        console.log(`Save data contains: level=${saveData.level}, money=${saveData.money}, health=${saveData.health}, kills=${saveData.kills}`);
        
        // Use findOneAndUpdate with upsert to ensure proper overwrite
        const updateData = {
            username: saveData.username,
            email: saveData.email || '',
            level: saveData.level || 1,
            experience: saveData.experience || 0,
            money: saveData.money || 0,
            health: saveData.health || 100,
            kills: saveData.kills || 0,
            lastLoginDate: new Date(),
            checkpoint: saveData.checkpoint || null,
            weapons: saveData.weapons || [],
            // Add save timestamp for debugging
            lastSaveTimestamp: new Date(),
            saveCount: { $inc: 1 } // Increment save counter for tracking
        };
        
        // Perform atomic update with full replacement of game data fields
        const updatedUser = await User.findOneAndUpdate(
            { _id: userId },
            { 
                $set: updateData,
                $inc: { saveCount: 1 }
            },
            { 
                new: true, // Return updated document
                upsert: false, // Don't create if doesn't exist
                runValidators: true // Run schema validation
            }
        );
        
        if (!updatedUser) {
            return res.status(404).json({ error: 'User not found' });
        }
        
        console.log(`Save successful for user ${updatedUser.username} - Save count: ${updatedUser.saveCount}`);
        
        // Return the updated user data in consistent format
        const responseData = {
            id: updatedUser._id.toString(),
            username: updatedUser.username,
            email: updatedUser.email || '',
            level: updatedUser.level || 1,
            experience: updatedUser.experience || 0,
            money: updatedUser.money || 0,
            health: updatedUser.health || 100,
            kills: updatedUser.kills || 0,
            lastLoginDate: updatedUser.lastLoginDate || new Date().toISOString(),
            checkpoint: updatedUser.checkpoint || null,
            weapons: updatedUser.weapons || [],
            saveCount: updatedUser.saveCount || 1
        };
        
        res.json(responseData);
        
    } catch (error) {
        console.error('Save error:', error);
        res.status(500).json({ error: 'Failed to save player data', details: error.message });
    }
});

// Enhanced alternative save endpoint for fallback
app.put('/api/player/data', auth, async (req, res) => {
    try {
        const userId = req.userId;
        const saveData = req.body;
        
        console.log(`Alternative save endpoint - Processing save for user ID: ${userId}`);
        
        // Use replaceOne for complete document replacement approach
        const filter = { _id: userId };
        const replacement = {
            _id: userId,
            username: saveData.username || saveData.userId,
            email: saveData.email || '',
            level: saveData.level || 1,
            experience: saveData.experience || 0,
            money: saveData.money || 0,
            health: saveData.health || 100,
            kills: saveData.kills || 0,
            lastLoginDate: new Date(),
            checkpoint: saveData.checkpoint || null,
            weapons: saveData.weapons || [],
            lastSaveTimestamp: new Date(),
            saveMethod: 'alternative_endpoint'
        };
        
        const result = await User.replaceOne(filter, replacement);
        
        if (result.matchedCount === 0) {
            return res.status(404).json({ error: 'User not found for alternative save' });
        }
        
        console.log(`Alternative save successful - Modified count: ${result.modifiedCount}`);
        
        // Fetch and return the updated data
        const updatedUser = await User.findById(userId);
        const responseData = {
            id: updatedUser._id.toString(),
            username: updatedUser.username,
            email: updatedUser.email || '',
            level: updatedUser.level || 1,
            experience: updatedUser.experience || 0,
            money: updatedUser.money || 0,
            health: updatedUser.health || 100,
            kills: updatedUser.kills || 0,
            lastLoginDate: updatedUser.lastLoginDate || new Date().toISOString(),
            checkpoint: updatedUser.checkpoint || null,
            weapons: updatedUser.weapons || []
        };
        
        res.json(responseData);
        
    } catch (error) {
        console.error('Alternative save error:', error);
        res.status(500).json({ error: 'Failed to save player data via alternative endpoint', details: error.message });
    }
});

// Enhanced GET endpoint to ensure consistent data format
app.get('/api/player/data', auth, async (req, res) => {
    try {
        const user = await User.findById(req.userId);
        if (!user) {
            return res.status(404).json({ error: 'User not found' });
        }
        
        // Ensure consistent response format
        const responseData = {
            id: user._id.toString(),
            username: user.username,
            email: user.email || '',
            level: user.level || 1,
            experience: user.experience || 0,
            money: user.money || 0,
            health: user.health || 100,
            kills: user.kills || 0,
            lastLoginDate: user.lastLoginDate || new Date().toISOString(),
            checkpoint: user.checkpoint || null,
            weapons: user.weapons || [],
            hasKey: user.hasKey || false
        };
        
        console.log(`Data fetch for user: ${user.username} - Level: ${responseData.level}, Money: ${responseData.money}, Health: ${responseData.health}`);
        
        res.json(responseData);
    } catch (error) {
        console.error('Player data fetch error:', error);
        res.status(500).json({ error: 'Failed to fetch player data' });
    }
});
