@file:Suppress("EXPERIMENTAL_API_USAGE","EXPERIMENTAL_UNSIGNED_LITERALS","PackageDirectoryMismatch","UnusedImport","unused","LocalVariableName","CanBeVal","PropertyName","EnumEntryName","ClassName","ObjectPropertyName","UnnecessaryVariable","SpellCheckingInspection")
package com.jetbrains.rd.ide.model

import com.jetbrains.rd.framework.*
import com.jetbrains.rd.framework.base.*
import com.jetbrains.rd.framework.impl.*

import com.jetbrains.rd.util.lifetime.*
import com.jetbrains.rd.util.reactive.*
import com.jetbrains.rd.util.string.*
import com.jetbrains.rd.util.*
import kotlin.time.Duration
import kotlin.reflect.KClass
import kotlin.jvm.JvmStatic



/**
 * #### Generated from [MyPluginModel.kt:8]
 */
class MyPluginModel private constructor(
    private val _isTestRunning: RdOptionalProperty<Boolean>,
    private val _testFrameworkDetected: RdSignal<String>,
    private val _getTestExecutionCommand: RdCall<String, String>
) : RdExtBase() {
    //companion
    
    companion object : ISerializersOwner {
        
        override fun registerSerializersCore(serializers: ISerializers)  {
        }
        
        
        @JvmStatic
        @JvmName("internalCreateModel")
        @Deprecated("Use create instead", ReplaceWith("create(lifetime, protocol)"))
        internal fun createModel(lifetime: Lifetime, protocol: IProtocol): MyPluginModel  {
            @Suppress("DEPRECATION")
            return create(lifetime, protocol)
        }
        
        @JvmStatic
        @Deprecated("Use protocol.myPluginModel or revise the extension scope instead", ReplaceWith("protocol.myPluginModel"))
        fun create(lifetime: Lifetime, protocol: IProtocol): MyPluginModel  {
            IdeRoot.register(protocol.serializers)
            
            return MyPluginModel()
        }
        
        
        const val serializationHash = 8787159488237779423L
        
    }
    override val serializersOwner: ISerializersOwner get() = MyPluginModel
    override val serializationHash: Long get() = MyPluginModel.serializationHash
    
    //fields
    val isTestRunning: IOptProperty<Boolean> get() = _isTestRunning
    val testFrameworkDetected: ISignal<String> get() = _testFrameworkDetected
    val getTestExecutionCommand: IRdCall<String, String> get() = _getTestExecutionCommand
    //methods
    //initializer
    init {
        _isTestRunning.optimizeNested = true
    }
    
    init {
        bindableChildren.add("isTestRunning" to _isTestRunning)
        bindableChildren.add("testFrameworkDetected" to _testFrameworkDetected)
        bindableChildren.add("getTestExecutionCommand" to _getTestExecutionCommand)
    }
    
    //secondary constructor
    private constructor(
    ) : this(
        RdOptionalProperty<Boolean>(FrameworkMarshallers.Bool),
        RdSignal<String>(FrameworkMarshallers.String),
        RdCall<String, String>(FrameworkMarshallers.String, FrameworkMarshallers.String)
    )
    
    //equals trait
    //hash code trait
    //pretty print
    override fun print(printer: PrettyPrinter)  {
        printer.println("MyPluginModel (")
        printer.indent {
            print("isTestRunning = "); _isTestRunning.print(printer); println()
            print("testFrameworkDetected = "); _testFrameworkDetected.print(printer); println()
            print("getTestExecutionCommand = "); _getTestExecutionCommand.print(printer); println()
        }
        printer.print(")")
    }
    //deepClone
    override fun deepClone(): MyPluginModel   {
        return MyPluginModel(
            _isTestRunning.deepClonePolymorphic(),
            _testFrameworkDetected.deepClonePolymorphic(),
            _getTestExecutionCommand.deepClonePolymorphic()
        )
    }
    //contexts
    //threading
    override val extThreading: ExtThreadingKind get() = ExtThreadingKind.Default
}
val IProtocol.myPluginModel get() = getOrCreateExtension(MyPluginModel::class) { @Suppress("DEPRECATION") MyPluginModel.create(lifetime, this) }

