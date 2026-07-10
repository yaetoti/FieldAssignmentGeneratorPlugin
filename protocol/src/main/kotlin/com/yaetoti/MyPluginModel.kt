package com.yaetoti

import com.jetbrains.rd.generator.nova.*

import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rider.model.nova.ide.IdeRoot

object MyPluginModel : Ext(IdeRoot) {
  init {
    property("isTestRunning", bool)
    signal("testFrameworkDetected", string)
    call("getTestExecutionCommand", string, string)
  }
}