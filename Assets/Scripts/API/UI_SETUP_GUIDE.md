# Login System UI Setup Guide

This guide will help you set up the UI for the Login/Register system in Unity.

## Prerequisites
1. Make sure TextMeshPro is imported in your project (It seems to be already imported in your project)
2. Ensure you have all the necessary scripts:
   - LoginUIManager.cs
   - LoginManager.cs
   - GameAPI.cs
   - UserDataModel.cs
   - UserDataSync.cs
   - AuthStartupManager.cs

## Creating Login Scene

1. Create a new scene named "Login"
   - Go to File > New Scene
   - Save it as "Login" in your Scenes folder

2. Set up Canvas
   - Create a new UI Canvas (Right-click Hierarchy > UI > Canvas)
   - Add EventSystem if it's not automatically created
   - Set Canvas Scaler (Script) properties:
     - UI Scale Mode: Scale With Screen Size
     - Reference Resolution: 1920 x 1080
     - Match: 0.5 (or your preferred value)

## Setting up Login UI Panels

### 1. Welcome Panel
Create a Panel for the initial welcome screen:
   - Create Panel under Canvas and name it "WelcomePanel"
   - Add background image (optional)
   - Add game title (TextMeshPro - Text)
   - Add version text (TextMeshPro - Text)
   - Add buttons:
     - Login Button (Button with TextMeshPro - Text)
     - Register Button (Button with TextMeshPro - Text)
     - Play Offline Button (Button with TextMeshPro - Text)

### 2. Login Panel
Create a Panel for the login screen:
   - Create Panel under Canvas and name it "LoginPanel"
   - Set it inactive initially (uncheck the checkbox in Inspector)
   - Add title (TextMeshPro - Text)
   - Add Username input field (TMP_InputField)
   - Add Password input field (TMP_InputField)
     - Set Content Type to "Password" for the password field
   - Add Remember Me toggle
   - Add Login button
   - Add Back button
   - Add Forgot Password button (optional)
   - Add Error Message text (TMP_Text) - keep it inactive initially

### 3. Register Panel
Create a Panel for the registration screen:
   - Create Panel under Canvas and name it "RegisterPanel" 
   - Set it inactive initially
   - Add title (TextMeshPro - Text)
   - Add Username input field (TMP_InputField)
   - Add Email input field (TMP_InputField)
   - Add Password input field (TMP_InputField)
     - Set Content Type to "Password"
   - Add Confirm Password input field (TMP_InputField)
     - Set Content Type to "Password"
   - Add Terms & Conditions toggle
   - Add Register button
   - Add Back button
   - Add Error Message text (TMP_Text) - keep it inactive initially

### 4. Loading Panel
Create a Panel for loading/progress display:
   - Create Panel under Canvas and name it "LoadingPanel"
   - Set it inactive initially
   - Add loading animation or spinner (optional)
   - Add Status text (TMP_Text)
   - Add Progress bar (Image with Image Type set to Filled)

### 5. Error Panel
Create a Panel for displaying errors:
   - Create Panel under Canvas and name it "ErrorPanel"
   - Set it inactive initially
   - Add error title (TMP_Text)
   - Add error message (TMP_Text)
   - Add Close button

## Adding Scripts to Objects

1. Add the LoginUIManager script to the Canvas:
   - Select the Canvas in the Hierarchy
   - Click "Add Component" in the Inspector
   - Search for "LoginUIManager" and add it

2. Configure LoginUIManager References:
   - Drag each panel from the Hierarchy to their corresponding fields in the LoginUIManager component
   - Assign all buttons, input fields, and text elements to their respective fields

3. Add the LoginManager script to an empty GameObject:
   - Create an empty GameObject and name it "LoginManager"
   - Add the LoginManager script to it
   - Configure any settings and references in the Inspector

4. Add AuthStartupManager (if needed):
   - Create an empty GameObject named "AuthStartupManager"
   - Add the AuthStartupManager script to it
   - Configure the scene names and settings

## Testing the Login System

1. Make sure your API_URL in GameAPI.cs is set to the correct server address
2. Set the Login scene as your starting scene in Build Settings
3. Enter Play mode to test your UI flows

## Troubleshooting

1. If buttons don't respond, check if the OnClick events are properly assigned
2. If text doesn't appear, make sure TextMeshPro components are used instead of legacy Text components
3. If API calls fail, verify your server is running and the API_URL is correct
4. Check the console for any script errors or exceptions

## Next Steps

1. Complete the Web API server implementation
2. Test the authentication flow end-to-end
3. Implement data synchronization with UserDataSync
4. Add game-specific features to save/load player progress
