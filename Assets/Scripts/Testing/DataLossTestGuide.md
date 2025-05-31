# Data Loss Testing Guide

## Quick Test Setup

### 1. **Server Setup** (Critical!)
Before testing, you MUST add the `/api/auth/verify` endpoint to your server:

```javascript
// Add this to your server file (e.g., server.js or app.js)
const jwt = require('jsonwebtoken');

// Helper function for consistent user data formatting
function formatUserResponse(user) {
  return {
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
}

// Add the missing /api/auth/verify endpoint
app.get('/api/auth/verify', auth, async (req, res) => {
  try {
    const user = await User.findById(req.userId);
    if (!user) {
      return res.status(404).json({ error: 'User not found' });
    }
    
    res.json(formatUserResponse(user));
  } catch (error) {
    console.error('Token verification error:', error);
    res.status(500).json({ error: 'Server error' });
  }
});
```

### 2. **Testing Scene Setup**

1. Create a new scene called "DataLossTest"
2. Add the `DataLossTestScript.cs` to a GameObject
3. Create UI elements:
   - Login Button
   - Logout Button  
   - Test Data Button
   - Validate Token Button
   - Username Input Field
   - Password Input Field
   - Status Text
   - User Data Text

### 3. **Test Procedures**

#### **Test 1: Basic Login/Logout**
```
1. Enter valid credentials
2. Click "Login" 
3. Verify user data appears
4. Click "Logout"
5. Check console for "✅ User data was saved during logout for recovery"
```

#### **Test 2: Data Persistence**
```
1. Login successfully
2. Click "Test Data" to modify and save data
3. Logout
4. Login again
5. Verify data matches what was saved
```

#### **Test 3: Token Validation**
```
1. Login successfully
2. Click "Validate Token"
3. Check console for validation results
4. Should show successful validation or fallback method
```

#### **Test 4: Complete Cycle** (Most Important!)
```
1. Login with test credentials
2. Right-click on DataLossTestScript in Inspector
3. Select "Test Complete Logout/Login Cycle"
4. Watch console for detailed results
5. Look for "✅ SUCCESS: Data persisted correctly"
```

### 4. **Expected Results**

#### **Before Fix (Old Behavior):**
- ❌ 404 errors spam console
- ❌ Data loss after logout/login cycles
- ❌ Token validation failures

#### **After Fix (New Behavior):**
- ✅ Minimal 404 logging (one line, not spam)
- ✅ Data preserved through logout/login cycles
- ✅ Fallback mechanisms work when `/api/auth/verify` missing
- ✅ Automatic data recovery from saved state

### 5. **Troubleshooting**

#### **If you still see data loss:**
1. Check that server has `/api/auth/verify` endpoint
2. Verify server returns consistent data format
3. Check Unity console for specific error messages
4. Ensure API_URL in GameAPI is correct

#### **If 404 errors persist:**
1. Server may not have all required endpoints
2. Check server logs for missing routes
3. Verify auth middleware is properly configured

#### **Server Endpoints Required:**
- `POST /api/auth/login`
- `GET /api/auth/verify` (NEW - was missing!)
- `GET /api/player/data`
- `PUT /api/player/save` or `PUT /api/player/data`

### 6. **Console Output Examples**

#### **Successful Test:**
```
[DataLossTest] Starting login test for user: testuser
[GameAPI] Login successful for user: testuser
[DataLossTest] ✅ Login successful!
[DataLossTest] Testing logout process...
[GameAPI] Saving logout state for user: testuser (ID: 507f1f77bcf86cd799439011)
[DataLossTest] ✅ User data was saved during logout for recovery
[DataLossTest] ✅ SUCCESS: Data persisted correctly through logout/login cycle!
```

#### **Fallback Working:**
```
[GameAPI] Auth verify endpoint not found (404), trying alternative method...
[GameAPI] Fallback token verification successful - ID: '507f1f77bcf86cd799439011', Username: 'testuser'
```

### 7. **Performance Notes**

The improved system:
- Reduces network requests by caching validation results
- Provides multiple fallback mechanisms
- Preserves data even when server endpoints are missing
- Logs errors appropriately without spamming console

### 8. **Next Steps After Testing**

1. **If tests pass:** Deploy to production and monitor
2. **If tests fail:** Check server implementation and Unity console errors
3. **For production:** Consider adding more robust error handling for specific game scenarios
