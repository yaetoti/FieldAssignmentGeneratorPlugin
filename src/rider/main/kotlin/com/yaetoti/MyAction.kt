package com.yaetoti

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.client.currentSession
import com.intellij.openapi.components.Service
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.util.lifetime
import com.intellij.openapi.ui.Messages
import com.jetbrains.rd.ide.model.MyPluginModel
import com.jetbrains.rd.ide.model.myPluginModel
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rider.projectView.solution

@Service(Service.Level.PROJECT)
class MyPluginModelHost(val project: Project) : LifetimedService() {
  val model: MyPluginModel? = project.solution.protocol?.myPluginModel
}

class MyAction : AnAction() {
  override fun actionPerformed(e: AnActionEvent) {
    println("START")
    val project = e.project
    val model = project!!.getService(MyPluginModelHost::class.java).model
    model!!.getTestExecutionCommand.start(project.lifetime, "Aboba").result
      .advise(project.lifetime) { taskResult ->
        // taskResult is an instance of RdTaskResult<T>
        val commandString = taskResult.unwrap()

        // Show the result
        println("AAAAAA")
        Messages.showInfoMessage(
          project,
          "Result from C#: $commandString",
          "Protocol Success!"
        )
      }

    println("SHOTS FIRED")
  }
}