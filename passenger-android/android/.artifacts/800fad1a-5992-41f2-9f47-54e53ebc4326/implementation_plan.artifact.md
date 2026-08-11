# Upgrade AGP to 8.5.0 and Gradle to 8.7

This plan outlines the steps to upgrade the Android project from Android Gradle Plugin (AGP) 7.4.2 and Gradle 7.4.2 to AGP 8.5.0 and Gradle 8.7. This upgrade is necessary to resolve compatibility issues with Kotlin 1.9 metadata and to support modern Android features (SDK 34+).

## User Review Required

> [!IMPORTANT]
> **JDK 17 Requirement**: AGP 8.x requires JDK 17 or higher to run. You must ensure your development environment (Android Studio Gradle settings and JAVA_HOME) is configured to use JDK 17.

> [!NOTE]
> This upgrade is compatible with your current Capacitor 6.0.0 dependencies.

## Proposed Changes

### Build Configuration

#### [MODIFY] [gradle-wrapper.properties](file:///C:/Programming/TransitPay-prototype/TransitPay-Prototype/passenger-android/android/gradle/wrapper/gradle-wrapper.properties)
*   Update `distributionUrl` to use Gradle 8.7.

#### [MODIFY] [build.gradle](file:///C:/Programming/TransitPay-prototype/TransitPay-Prototype/passenger-android/android/build.gradle) (Root)
*   Update `com.android.tools.build:gradle` to version `8.5.0`.

#### [MODIFY] [app/build.gradle](file:///C:/Programming/TransitPay-prototype/TransitPay-Prototype/passenger-android/android/app/build.gradle)
*   Verify that `namespace` is present (confirmed: it is).
*   AGP 8.x removes support for some legacy options, but the current file appears compatible.

## Verification Plan

### Automated Tests
*   Run `./gradlew :app:assembleDebug` to verify the build completes successfully.
*   Run `./gradlew :app:dependencies` to check for any dependency resolution issues.

### Manual Verification
*   Sync project with Gradle files in Android Studio.
*   Verify that the "mergeExtDexDebug" error is resolved.
