import groovy.ant.FileNameFinder
import org.apache.tools.ant.taskdefs.condition.Os
import org.gradle.api.internal.artifacts.transform.UnzipTransform
import org.jetbrains.intellij.platform.gradle.Constants
import org.jetbrains.intellij.platform.gradle.utils.asPath
import org.jetbrains.kotlin.gradle.dsl.JvmTarget
import java.time.LocalTime

plugins {
    id("java")
    alias(libs.plugins.kotlinJvm)
    // See https://github.com/JetBrains/intellij-platform-gradle-plugin/releases
    id("org.jetbrains.intellij.platform") version "2.17.0"
    id("me.filippov.gradle.jvm.wrapper") version "0.14.0"
}

val isWindows = Os.isFamily(Os.FAMILY_WINDOWS)
extra["isWindows"] = isWindows

val DotnetSolution: String by project
val BuildConfiguration: String by project
val ProductVersion: String by project
val RiderModelVersion: String by project
val DotnetPluginId: String by project
val RiderPluginId: String by project
val PublishToken: String by project

allprojects {
    repositories {
        maven { setUrl("https://cache-redirector.jetbrains.com/maven-central") }
        // Needed for the rider model
        maven { setUrl("https://cache-redirector.jetbrains.com/intellij-repository/releases") }
        maven { setUrl("https://cache-redirector.jetbrains.com/intellij-repository/snapshots") }
    }
}

repositories {
    intellijPlatform {
        defaultRepositories()
        jetbrainsRuntime()
    }
}

tasks.wrapper {
    gradleVersion = "9.0.0"
    distributionType = Wrapper.DistributionType.ALL
    distributionUrl = "https://cache-redirector.jetbrains.com/services.gradle.org/distributions/gradle-${gradleVersion}-all.zip"
}

version = extra["PluginVersion"] as String

// TODO Debug
println("Build Directory: ${layout.buildDirectory.get().asPath}")

tasks.processResources {
    from("dependencies.json") { into("META-INF") }
}

sourceSets {
    main {
        java.srcDir("src/rider/main/java")
        kotlin.srcDir("src/rider/main/kotlin")
        resources.srcDir("src/rider/main/resources")
    }
}

// Unzipper
//@CacheableTransform
//abstract class RiderRdTransform : TransformAction<TransformParameters.None> {
//    @get:Inject
//    abstract val archives: ArchiveOperations
//
//    @get:Inject
//    abstract val fs: FileSystemOperations
//
//    @get:InputArtifact
//    @get:PathSensitive(PathSensitivity.NONE)
//    abstract val inputArtifact: Provider<FileSystemLocation>
//
//    override fun transform(outputs: TransformOutputs) {
//        val input = inputArtifact.get().asFile
//        val outputDir = outputs.dir(input.nameWithoutExtension)
//
//        fs.copy {
//            includeEmptyDirs = false
//            from(archives.zipTree(input))
//            into(outputDir)
//        }
//    }
//}

//dependencies.registerTransform(RiderRdTransform::class) {
//    from.attribute(
//        ArtifactTypeDefinition.ARTIFACT_TYPE_ATTRIBUTE,
//        ArtifactTypeDefinition.ZIP_TYPE
//    )
//    to.attribute(
//        ArtifactTypeDefinition.ARTIFACT_TYPE_ATTRIBUTE,
//        ArtifactTypeDefinition.DIRECTORY_TYPE
//    )
//}

// A model artifact separate from rider
val riderModelSource: Configuration by configurations.creating {
    isCanBeConsumed = false
    isCanBeResolved = true
    isTransitive = false
}

dependencies {
    riderModelSource("com.jetbrains.intellij.rider:riderRD:2026.1.4@zip")
}


val extractedRiderModelJar = layout.buildDirectory.file("riderModel/rider-model.jar")

val extractRiderModelJar by tasks.registering(Copy::class) {
    val riderRdZip = riderModelSource.elements.map { elements ->
        elements.single().asFile
    }

    from(riderRdZip.map { zipFile ->
        zipTree(zipFile).matching {
            include("lib/rd/rider-model.jar")
        }
    })

    into(layout.buildDirectory.dir("riderModel"))

    eachFile {
        path = "rider-model.jar"
    }

    includeEmptyDirs = false
}

val riderModel: Configuration by configurations.creating {
    isCanBeConsumed = true
    isCanBeResolved = false
}

artifacts {
    add(riderModel.name, extractedRiderModelJar) {
        type = "jar"
        extension = "jar"
        classifier = "rider-model"
        builtBy(extractRiderModelJar)
    }
}


//val riderModelJar = layout.buildDirectory.file("riderModel/rider-model.jar")

//artifacts {
//    add(riderModel.name, provider {
//        println("Vibe Check")
//        println("Default Time: ${LocalTime.now()}")
//        val riderRdDir = riderModelSource.singleFile
//        println("Got file")
//        println("Default Time: ${LocalTime.now()}")
//        val riderModelJar = riderRdDir.resolve("lib/rd/rider-model.jar")
//        println("Resolved")
//        println("Default Time: ${LocalTime.now()}")
//
//        check(riderRdDir.isDirectory) {
//            "Expected transformed riderRD artifact to be a directory, got: $riderRdDir"
//        }
//
//        check(riderModelJar.isFile) {
//            "Expected rider model jar at '$riderModelJar', but it does not exist"
//        }
//
//        println("Vibe Checked 🤩🥰")
//
//        riderModelJar
//    }) {
//        type = "jar"
//        extension = "jar"
//        classifier = "rider-model"
//    }
//}

dependencies {
    intellijPlatform {
        clion(ProductVersion)
        jetbrainsRuntime()

        bundledPlugin("com.intellij.clion")
        bundledPlugin("org.jetbrains.plugins.clion.radler")



        // TODO: add plugins
        // bundledPlugin("uml")
        // bundledPlugin("com.jetbrains.ChooseRuntime:1.0.9")
    }
}


//val riderModelJar = layout.buildDirectory.file("riderModel/rider-model.jar")
//
//val extractRiderModelJar by tasks.registering(Copy::class) {
//    println("Vibe Check")
//    val riderRdZip = riderModelSource.elements.map { elements ->
//        elements.single().asFile
//    }
//
//    println("Got file")
//
//    from(riderRdZip.map { zipFile ->
//        zipTree(zipFile).matching {
//            include("lib/rd/rider-model.jar")
//        }
//    })
//
//    println("Resolved")
//
//    into(layout.buildDirectory.dir("riderModel"))
//    eachFile {
//        path = "rider-model.jar"
//    }
//    includeEmptyDirs = false
//
//    println("Vibe Checked 🥰🤩")
//}
//
//artifacts {
//    add(riderModel.name, riderModelJar) {
//        type = "jar"
//        extension = "jar"
//        classifier = "rider-model"
//        builtBy(extractRiderModelJar)
//    }
//}





//val extractRiderModelJar by tasks.registering(Copy::class) {
//    from(riderModelSource.map { riderRdDirs ->
//        val riderRdDir = riderRdDirs
//        val riderModelJar = riderRdDir.resolve("lib/rd/rider-model.jar")
//
//        check(riderRdDir.isDirectory) {
//            "Expected transformed riderRD artifact to be a directory, got: $riderRdDir"
//        }
//
//        check(riderModelJar.isFile) {
//            "Expected rider model jar at '$riderModelJar', but it does not exist"
//        }
//
//        riderModelJar
//    })
//
//    into(layout.buildDirectory.dir("riderModel"))
//    rename { "rider-model.jar" }
//}
//
//artifacts {
//    add(riderModel.name, riderModelJar) {
//        type = "jar"
//        extension = "jar"
//        classifier = "rider-model"
//        builtBy(extractRiderModelJar)
//    }
//}





//// A model artifact separate from rider
//val riderModelSource: Configuration by configurations.creating {
//    isCanBeConsumed = false
//    isCanBeResolved = true
//    isTransitive = false
//}
//
//dependencies {
//    riderModelSource("com.jetbrains.intellij.rider:riderRD:2026.1.4")
//    // TODO Seems like it uses riderRD, not rider model generated
//    //riderModelSource("com.jetbrains.intellij.rider:rider-model-generated:$RiderModelVersion")
//}
//
//val riderModel: Configuration by configurations.creating {
//    isCanBeConsumed = true
//    isCanBeResolved = false
//}
//
//artifacts {
//    add(riderModel.name, provider {
//        println("=== Files in riderModelSource ===")
//        riderModelSource.files.forEach { println(it.name) }
//        println("================================")
//        println("Count: ${riderModelSource.files.size}")
//
//        riderModelSource.singleFile.also {
//            check(it.isFile) {
//                "Rider model artifact is not resolved from configuration '$riderModelSource'. Check RiderModelVersion=$RiderModelVersion and IntelliJ repositories."
//            }
//        }
//    }) {
//        builtBy(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
//    }
//}

// Tasks

tasks.compileKotlin {
    compilerOptions { jvmTarget.set(JvmTarget.JVM_21) }
}

val setBuildTool by tasks.registering {
    doLast {
        extra["executable"] = "dotnet"
        var args = mutableListOf("msbuild")

        if (isWindows) {
            val result = providers.exec {
                executable("${rootDir}\\tools\\vswhere.exe")
                args("-latest", "-property", "installationPath", "-products", "*")
                workingDir(rootDir)
            }

            val directory = result.standardOutput.asText.get().trim()
            if (directory.isNotEmpty()) {
                val files = FileNameFinder().getFileNames("${directory}\\MSBuild", "**/MSBuild.exe")
                extra["executable"] = files.get(0)
                args = mutableListOf("/v:minimal")
            }
        }

        args.add("${DotnetSolution}")
        args.add("/p:Configuration=${BuildConfiguration}")
        args.add("/p:HostFullIdentifier=")
        extra["args"] = args
    }
}

val compileDotNet by tasks.registering {
    dependsOn(setBuildTool)
    doLast {
        val executable: String by setBuildTool.get().extra
        val arguments = (setBuildTool.get().extra["args"] as List<String>).toMutableList()
        arguments.add("/t:Restore;Rebuild")
        providers.exec {
            executable(executable)
            args(arguments)
            workingDir(rootDir)
        }
    }
}

val testDotNet by tasks.registering {
    doLast {
        providers.exec {
            executable("dotnet")
            args("test","${DotnetSolution}","--logger","GitHubActions")
            workingDir(rootDir)
        }
    }
}

tasks.buildPlugin {
    doLast {
        copy {
            from("${layout.buildDirectory}/distributions/${rootProject.name}-${version}.zip")
            into("${rootDir}/output")
        }

        // TODO: See also org.jetbrains.changelog: https://github.com/JetBrains/gradle-changelog-plugin
        val changelogText = file("${rootDir}/CHANGELOG.md").readText()
        val changelogMatches = Regex("(?s)(-.+?)(?=##|$)").findAll(changelogText)
        val changeNotes = changelogMatches.map {
            it.groups[1]!!.value.replace("(?s)- ".toRegex(), "\u2022 ").replace("`", "").replace(",", "%2C").replace(";", "%3B")
        }.take(1).joinToString()

        val executable: String by setBuildTool.get().extra
        val arguments = (setBuildTool.get().extra["args"] as List<String>).toMutableList()
        arguments.add("/t:Pack")
        arguments.add("/p:PackageOutputPath=${rootDir}/output")
        arguments.add("/p:PackageReleaseNotes=${changeNotes}")
        arguments.add("/p:PackageVersion=${version}")
        providers.exec {
            executable(executable)
            args(arguments)
            workingDir(rootDir)
        }
    }
}

tasks.runIde {
    // Match Rider's default heap size of 1.5Gb (default for runIde is 512Mb)
    maxHeapSize = "1500m"
}

tasks.patchPluginXml {
    // TODO: See also org.jetbrains.changelog: https://github.com/JetBrains/gradle-changelog-plugin
    val changelogText = file("${rootDir}/CHANGELOG.md").readText()
    val changelogMatches = Regex("(?s)(-.+?)(?=##|\$)").findAll(changelogText)

    changeNotes.set(changelogMatches.map {
        it.groups[1]!!.value.replace("(?s)\r?\n".toRegex(), "<br />\n")
    }.take(1).joinToString())
}

tasks.prepareSandbox {
    dependsOn(compileDotNet)

    val outputFolder = "${rootDir}/src/dotnet/${DotnetPluginId}/bin/${DotnetPluginId}.Rider/${BuildConfiguration}"
    val dllFiles = listOf(
            "$outputFolder/${DotnetPluginId}.dll",
            "$outputFolder/${DotnetPluginId}.pdb",

            // TODO: add additional assemblies
    )

    dllFiles.forEach({ f ->
        val file = file(f)
        from(file, { into("${rootProject.name}/dotnet") })
    })

    doLast {
        dllFiles.forEach({ f ->
            val file = file(f)
            if (!file.exists()) throw RuntimeException("File ${file} does not exist")
        })
    }
}

tasks.publishPlugin {
    dependsOn(testDotNet)
    dependsOn(tasks.buildPlugin)
    token.set("${PublishToken}")

    doLast {
        providers.exec {
            executable("dotnet")
            args("nuget","push","output/${DotnetPluginId}.${version}.nupkg","--api-key","${PublishToken}","--source","https://plugins.jetbrains.com")
            workingDir(rootDir)
        }
    }
}
