plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.plugin.compose")
}
android {
    namespace = "hr.brendigo.clanarina"
    compileSdk = 36
    defaultConfig {
        applicationId = "hr.brendigo.clanarina"
        minSdk = 26
        targetSdk = 36
        versionCode = (providers.gradleProperty("releaseVersionCode").orNull?.toIntOrNull() ?: 1)
        versionName = providers.gradleProperty("releaseVersionName").orNull ?: "0.1.0-dev"
    }
    buildFeatures { compose = true }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    val ksPath = System.getenv("CLANARINA_ANDROID_KEYSTORE")
    if (!ksPath.isNullOrBlank()) {
        signingConfigs {
            create("distribution") {
                storeFile = file(ksPath)
                storePassword = System.getenv("CLANARINA_ANDROID_STORE_PASSWORD")
                keyAlias = System.getenv("CLANARINA_ANDROID_KEY_ALIAS")
                keyPassword = System.getenv("CLANARINA_ANDROID_KEY_PASSWORD")
            }
        }
        buildTypes {
            getByName("release") {
                signingConfig = signingConfigs.getByName("distribution")
                isMinifyEnabled = false
            }
        }
    }
}
kotlin { compilerOptions { jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17) } }
dependencies {
    val composeBom = platform("androidx.compose:compose-bom:2026.09.00")
    implementation(composeBom)
    implementation("androidx.activity:activity-compose:1.13.0")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.foundation:foundation")
    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.ui:ui-tooling-preview")
    debugImplementation("androidx.compose.ui:ui-tooling")
}
