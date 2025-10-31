# APK Signing Guide

Your APK is currently unsigned. For production releases, you should sign your APK.

## Why Sign APKs?

- **Required for Google Play Store**
- **Security**: Verifies the APK hasn't been tampered with
- **Updates**: Users can only update from the same signing key
- **Trust**: Shows the app comes from you

## Quick Setup (5 minutes)

### 1. Generate Keystore (One Time Only)

```cmd
keytool -genkey -v -keystore lanflix-release.keystore ^
    -alias lanflix ^
    -keyalg RSA ^
    -keysize 2048 ^
    -validity 10000
```

Answer the prompts:
- **Password**: Choose a strong password (remember it!)
- **Name**: Your name or company
- **Organization**: Your organization
- **City, State, Country**: Your location

**IMPORTANT**: Keep this keystore file safe! You'll need it for all future updates.

### 2. Move Keystore to Safe Location

```cmd
move lanflix-release.keystore frontend\build-tools\android\
```

### 3. Create keystore.properties

Create `frontend\build-tools\android\android\keystore.properties`:

```properties
storeFile=..\\..\\..\\lanflix-release.keystore
storePassword=YOUR_STORE_PASSWORD
keyAlias=lanflix
keyPassword=YOUR_KEY_PASSWORD
```

Replace `YOUR_STORE_PASSWORD` and `YOUR_KEY_PASSWORD` with your actual passwords.

**IMPORTANT**: Add this file to `.gitignore` - never commit passwords!

### 4. Configure build.gradle

Edit `frontend\build-tools\android\android\app\build.gradle`:

Find the `android {` section and add:

```gradle
android {
    ...
    
    signingConfigs {
        release {
            def keystorePropertiesFile = rootProject.file("keystore.properties")
            if (keystorePropertiesFile.exists()) {
                def keystoreProperties = new Properties()
                keystoreProperties.load(new FileInputStream(keystorePropertiesFile))

                storeFile file(keystoreProperties['storeFile'])
                storePassword keystoreProperties['storePassword']
                keyAlias keystoreProperties['keyAlias']
                keyPassword keystoreProperties['keyPassword']
            }
        }
    }
    
    buildTypes {
        release {
            signingConfig signingConfigs.release
            minifyEnabled false
            proguardFiles getDefaultProguardFile('proguard-android-optimize.txt'), 'proguard-rules.pro'
        }
    }
}
```

### 5. Build Signed APK

```cmd
cd frontend\build-tools\android
build-apk.bat
```

Now your APK will be signed automatically!

## Verify Signing

Check if your APK is signed:

```cmd
jarsigner -verify -verbose -certs releases\lanflix-android-v1.0.0.apk
```

Should show: `jar verified.`

## Security Best Practices

### Protect Your Keystore

1. **Backup**: Keep multiple copies in secure locations
2. **Never commit**: Add to `.gitignore`
3. **Encrypt**: Store in encrypted storage
4. **Document**: Write down passwords in a secure password manager

### .gitignore

Add to your `.gitignore`:

```gitignore
# Keystore files
*.keystore
*.jks
keystore.properties

# APK files (optional - you might want to commit releases)
*.apk
```

## Troubleshooting

### "keystore.properties not found"

Create the file as shown in step 3.

### "Keystore was tampered with, or password was incorrect"

Check your password in `keystore.properties`.

### "Cannot recover key"

Your key password might be wrong. Check `keyPassword` in `keystore.properties`.

### Lost Keystore?

If you lose your keystore:
- You **cannot** update existing app installations
- You must create a new keystore
- Users must uninstall and reinstall
- **This is why backups are critical!**

## For CI/CD

Store keystore and passwords as secrets:

### GitHub Actions

1. Base64 encode your keystore:
   ```cmd
   certutil -encode lanflix-release.keystore keystore.b64
   ```

2. Add to GitHub Secrets:
   - `KEYSTORE_BASE64`: The base64 content
   - `KEYSTORE_PASSWORD`: Your store password
   - `KEY_PASSWORD`: Your key password
   - `KEY_ALIAS`: `lanflix`

3. In workflow, decode and use:
   ```yaml
   - name: Decode Keystore
     run: |
       echo "${{ secrets.KEYSTORE_BASE64 }}" | base64 -d > keystore.jks
   ```

## Alternative: Android Studio

If you prefer a GUI:

1. Open project in Android Studio
2. Build → Generate Signed Bundle / APK
3. Select APK
4. Create new keystore or use existing
5. Fill in details
6. Choose release build variant
7. Click Finish

## Quick Reference

```cmd
# Generate keystore
keytool -genkey -v -keystore lanflix-release.keystore -alias lanflix -keyalg RSA -keysize 2048 -validity 10000

# Verify APK is signed
jarsigner -verify -verbose -certs your-app.apk

# View keystore info
keytool -list -v -keystore lanflix-release.keystore

# Change keystore password
keytool -storepasswd -keystore lanflix-release.keystore
```

## Support

For more information:
- [Android App Signing](https://developer.android.com/studio/publish/app-signing)
- [Gradle Signing Config](https://developer.android.com/studio/build/gradle-tips#sign-your-app)
