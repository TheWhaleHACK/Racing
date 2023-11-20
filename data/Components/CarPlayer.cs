/* Copyright (C) 2005-2023, UNIGINE. All rights reserved.
*
* This file is a part of the UNIGINE 2 SDK.
*
* Your use and / or redistribution of this software in source and / or
* binary form, with or without modification, is subject to: (i) your
* ongoing acceptance of and compliance with the terms and conditions of
* the UNIGINE License Agreement; and (ii) your inclusion of this notice
* in any version of this software that you use or redistribute.
* A copy of the UNIGINE License Agreement is available by contacting
* UNIGINE. at http://unigine.com/
*/

using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "af999e850a3f9c19f2739d911751d46d1149e4d3")]
public class CarPlayer : Car
{
     protected override void Update()
    {
        // установка поворота руля
        float wheel = 0.0f;
       
        wheel -= InputController.GetAction(InputController.InputActionType.WheelRight);
        wheel += InputController.GetAction(InputController.InputActionType.WheelLeft);
       
        SetWheelPosition(wheel);
       
        // тормоз и газ
        float brake = InputController.GetAction(InputController.InputActionType.Brake);
        float throttle = InputController.GetAction(InputController.InputActionType.Throttle);
       
        // изменение режима движения при смене знака скорости и от состояния тормоза и газа
        float velocity = Speed * (CurrentMoveDirection == MoveDirection.Forward ? 1 : -1);
       
        if (velocity > 0.0f)
        {
            if (velocity < 1.25f && brake > MathLib.EPSILON && throttle < MathLib.EPSILON)
                SetMoveDirection(MoveDirection.Reverse);
        }
        else
        {
            if (velocity > -1.25f && throttle > MathLib.EPSILON && brake < MathLib.EPSILON)
                SetMoveDirection(MoveDirection.Forward);
        }
       
        // при заднем движении газ и тормоз меняются местами
        if (CurrentMoveDirection == MoveDirection.Reverse)
        {
            float t = brake;
            brake = throttle;
            throttle = t;
        }
       
        // установка газа и тормоза
        SetThrottle(throttle);
        SetBrake(brake);
        // установка стояночного тормоза клавишей и при неактивном контроллере ввода (после завершения игры)
        SetHandBrake(InputController.IsEnabled ? InputController.GetAction(InputController.InputActionType.HandBrake) : 1.0f);
       
        base.Update();
    
	}
}