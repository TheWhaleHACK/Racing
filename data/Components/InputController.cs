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

[Component(PropertyGuid = "84277d310c17c19f960fe47d3b007d76e7b1151f")]
public class InputController : Component
{
	// данный компонент представлен синглтоном для упрощения доступа
    private static InputController instance = null;
   
    // определим список типов действий: нажатие на газ, тормоз, поворот, стояночный тормоз и сброс позиции
    public enum InputActionType
    {
        Throttle = 0,
        Brake,
        WheelLeft,
        WheelRight,
        HandBrake,
        Reset,
    }
   
    // интерфейс состояния единицы ввода
    private interface IInputState
    {
        float State { get; }
    }
   
    // реализации интерфейса для зажатой и нажатой клавиши
    private class InputStateKeyboardPressed : IInputState {
       
        private Input.KEY key = Input.KEY.UNKNOWN;
       
        public InputStateKeyboardPressed(Input.KEY key) { this.key = key; }
        float IInputState.State { get { return Input.IsKeyPressed(key) ? 1.0f : 0.0f; } }
    }
   
    private class InputStateKeyboardDown : IInputState {
       
        private Input.KEY key = Input.KEY.UNKNOWN;
       
        public InputStateKeyboardDown(Input.KEY key) { this.key = key; }
        float IInputState.State { get { return Input.IsKeyDown(key) ? 1.0f : 0.0f; } }
    }
   
    // абстракция для действия ввода, принимает список состояний при инициализации
    // и обновляет собственное состояние, равное 1.0f, если хотя бы одно состояние из списка выполняется
    private class InputAction
    {
        private IInputState[] states;
        private float state = 0.0f;
       
        public InputAction(IInputState[] states) { this.states = states; }
       
        public void Update()
        {
            float s = float.NegativeInfinity;
            foreach(IInputState state in states)
            {
                s = MathLib.Max(s,state.State);
            }
           
            state = s;
        }
       
        public float State { get { return state; } }
    }
   
    // определим список действий ввода для клавиш управления
    private InputAction[] actions =
    {
        new InputAction(new IInputState[] { new InputStateKeyboardPressed(Input.KEY.W) }),
        new InputAction(new IInputState[] { new InputStateKeyboardPressed(Input.KEY.S) }),
        new InputAction(new IInputState[] { new InputStateKeyboardPressed(Input.KEY.A) }),
        new InputAction(new IInputState[] { new InputStateKeyboardPressed(Input.KEY.D) }),
        new InputAction(new IInputState[] { new InputStateKeyboardPressed(Input.KEY.SPACE) }),
        new InputAction(new IInputState[] { new InputStateKeyboardDown(Input.KEY.F5) }),
    };
   
    // инициализация контроллера ввода будет проведена в первую очередь благодаря явно указанному порядку
	[Method(Order=0)]

	private void Init()
	{
		// write here code to be called on component initialization
		instance = this;
        IsEnabled = true;
	}
	
	// обновление состояний каждого действия каждый кадр
	
	private void Update()
	{
		// write here code to be called before updating each render frame
		foreach(InputAction action in actions)
        {
            action.Update();
            
        }
	}

	public static float GetAction(InputActionType action)
    {
        if (!IsEnabled)
            return 0.0f;
        if (instance == null)
            return 0.0f;
        return instance.actions[(int)action].State;
    }
   
    public static bool IsEnabled { get; set; }
}