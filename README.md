# Dyslexia Text Motion Prototype

A Unity + TextMeshPro prototype for unstable text motion inspired by dyslexia-like reading difficulties.

本项目是一个基于 Unity 和 TextMeshPro 的文字视觉扰动原型。  
它通过逐字符控制文字的位置、旋转、缩放、拖尾和回归，让读者体验一种文字难以稳定聚焦的阅读状态。

## Project Status

This project is an unfinished prototype.

目前项目仍是半成品，代码结构和运动逻辑还不完善，但已经可以展示核心概念。  
短期内我暂时不会继续重构，因此先开源出来，供有兴趣的人参考、修改或继续实现。

## Features

- Character-level animation based on TextMeshPro mesh vertices
- Per-character movement states: idle, moving, spinning, and returning
- Gaze-point-based text disturbance area
- Smooth character motion, rotation, scaling, and return behavior
- Extra offset effect to create a dragged or unstable reading feeling
- Designed as an experimental prototype for accessibility-related visual interaction

## 功能特点

- 基于 TextMeshPro Mesh 顶点的逐字符动画
- 每个字符拥有独立状态：静止、移动、旋转、回归
- 根据注视点附近区域触发文字扰动
- 支持字符移动、旋转、缩放和回到原位
- 通过额外偏移制造文字拖尾和不稳定阅读感
- 可作为可访问性相关视觉交互实验原型

## Important Note

This project is not a medical tool.

Dyslexia is a complex reading difference that may involve phonological processing, reading fluency, working memory, attention, and other cognitive factors. This prototype only explores one visual interaction effect: unstable and disturbed text motion.

The goal of this project is not to accurately reproduce dyslexia, but to provide an experimental way for viewers to experience how visual instability may make reading more difficult.

## 重要说明

本项目不是医学工具。

阅读障碍是一种复杂的阅读差异，可能涉及音韵处理、阅读流畅度、工作记忆、注意力等多方面因素。本项目只探索其中一种视觉交互效果：文字的不稳定运动与视觉干扰。

本项目的目标不是准确复现阅读障碍，而是通过一个实验性原型，让观看者感受视觉不稳定如何增加阅读难度。

## Built With

- Unity
- TextMeshPro
- C#

## How to Use

1. Clone or download this repository.
2. Open the project with Unity.
3. Open the demo scene.
4. Run the scene and observe the text movement effect.

## License

MIT License
