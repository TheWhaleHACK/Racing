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

[Component(PropertyGuid = "b5948397ae56af3c72d25203f5d149fedf13ee15")]
public class Car : Component
{
	 // определим два режима движения: вперёд и назад
    protected enum MoveDirection
    {
        Forward,
        Reverse,
    }
   
    // параметры автомобиля: ускорение, максимальная скорость и поворот руля, крутящий момент
    public float acceleration = 50.0f;
    public float max_velocity = 90.0f;
    private float max_turn_angle = 30.0f;
    public float default_torque = 5.0f;
    // длина и ширина кузова
    public float car_base = 3.0f;
    public float car_width = 2.0f;
    // быстрота газа, тормоза и поворота
    public float throttle_speed = 2.0f;
    public float brake_speed = 1.2f;
    public float wheel_speed = 2.0f;
    // сила рабочего и стояночного тормоза
    public float brake_damping = 8.0f;
    public float hand_brake_damping = 30.0f;
    // ссылки на ноды колес
    public Node wheel_fl = null;
    public Node wheel_fr = null;
    public Node wheel_rl = null;
    public Node wheel_rr = null;
   
    // ссылки на ноды светотехники: стоп-сигнал и фонарь заднего хода
    public Node brake_light = null;
    public Node reverse_light = null;
    // колесные сочленения
    private JointWheel joint_wheel_fl = null;
    private JointWheel joint_wheel_fr = null;
    private JointWheel joint_wheel_rl = null;
    private JointWheel joint_wheel_rr = null;
   
    // определим желаемые и текущие значения для газа, тормоза, руля и стояночного тормоза
    private float target_throttle = 0.0f;
    private float target_brake = 0.0f;
    private float target_wheel = 0.0f;
    private float target_hand_brake = 0.0f;
   
    private float current_throttle = 0.0f;
    private float current_brake = 0.0f;
    private float current_wheel = 0.0f;
    private float current_hand_brake = 0.0f;
   
    // по умолчанию автомобиль в режиме движения вперёд
    private MoveDirection current_move_direction = MoveDirection.Forward;
   
    // переменные для текущей скорости вращения, крутящего момента и угла поворота
    private float current_velocity = 0.0f;
    private float current_torque = 0.0f;
    private float current_turn_angle = 0.0f;
   
    // физическое тело кузова
    private BodyRigid CarBodyRigid = null;
   
    private void Init()
    {
        // при инициализации получаем колесные сочленения и физическое тело кузова
        if (wheel_rl)
            joint_wheel_rl = wheel_rl.ObjectBody.GetJoint(0) as JointWheel;
       
        if (wheel_rr)
            joint_wheel_rr = wheel_rr.ObjectBody.GetJoint(0) as JointWheel;
       
        if (wheel_fl)
            joint_wheel_fl = wheel_fl.ObjectBody.GetJoint(0) as JointWheel;
       
        if (wheel_fr)
            joint_wheel_fr = wheel_fr.ObjectBody.GetJoint(0) as JointWheel;
       
        CarBodyRigid = node.ObjectBodyRigid;
    }
    protected virtual void Update()
    {
        // используем время рендера предыдущего кадра, чтобы не зависеть от FPS
        float deltaTime = Game.IFps;
       
        // плавно изменяем текущие газ, тормоз и положение руля в сторону требуемых
        current_throttle = MathLib.MoveTowards(current_throttle, target_throttle, throttle_speed * deltaTime);
        current_brake = MathLib.MoveTowards(current_brake, target_brake, brake_speed * deltaTime);
        current_wheel = MathLib.MoveTowards(current_wheel, target_wheel, wheel_speed * deltaTime);
        current_hand_brake = MathLib.MoveTowards(current_hand_brake, target_hand_brake, brake_speed * deltaTime);
       
        // включаем ноду стоп-сигнала, если тормоз активен (значение больше ~нуля)
        if (brake_light != null)
            brake_light.Enabled = target_brake > MathLib.EPSILON;
        // текущее значение крутящего момента вычисляется как произведение положения газа и стандартного множителя
        current_torque = default_torque * current_throttle;
        // при нажатии на газ
        if (current_throttle > MathLib.EPSILON)
        {
            // текущая угловая скорость колес изменяется согласно ускорению и направлению движения
            current_velocity += deltaTime * MathLib.Lerp(0.0f, acceleration, current_throttle) 
                                          * (current_move_direction == MoveDirection.Forward ? 1.0f : -1.0f);
        }
        else
        {
            // в противном случае снижаем скорость экспоненциально
            current_velocity *= MathLib.Exp(-deltaTime);
        }
        // вычисляем силу тормозов в зависимости от их текущей интенсивности
        float damping = MathLib.Lerp(0.0f, brake_damping, current_brake);
        float rdamping = MathLib.Lerp(0.0f, hand_brake_damping, current_hand_brake);
        // применяем торможение для всех колес, для задних колес применяется также и стояночный тормоз
        joint_wheel_fl.AngularDamping = damping;
        joint_wheel_fr.AngularDamping = damping;
        joint_wheel_rl.AngularDamping = MathLib.Max(damping, rdamping);
        joint_wheel_rr.AngularDamping = MathLib.Max(damping, rdamping);
        // вычисляем текущие угловую скорость и угол поворота, ограничив крайние значения
        current_velocity = MathLib.Clamp(current_velocity, -max_velocity, max_velocity);
        current_turn_angle = MathLib.Lerp(-max_turn_angle, max_turn_angle, MathLib.Clamp(0.5f + current_wheel * 0.5f, 0.0f,1.0f));
        // симуляция дифференциала для передней оси: колеса должны повернуться на различный угол
        float angle_0 = current_turn_angle;
        float angle_1 = current_turn_angle;
        if (MathLib.Abs(current_turn_angle) > MathLib.EPSILON)
        {
            float radius = car_base / MathLib.Tan(current_turn_angle * MathLib.DEG2RAD);
            float radius_0 = radius - car_width * 0.5f;
            float radius_1 = radius + car_width * 0.5f;
            angle_0 = MathLib.Atan(car_base / radius_0) * MathLib.RAD2DEG;
            angle_1 = MathLib.Atan(car_base / radius_1) * MathLib.RAD2DEG;
        }
        // применяем поворот для обоих передних колес при помощи матрицы поворота вдоль оси Z
        joint_wheel_fr.Axis10 = MathLib.RotateZ(angle_1).GetColumn3(0);
        joint_wheel_fl.Axis10 = MathLib.RotateZ(angle_0).GetColumn3(0);
    }
    // параметры физических объектов важно изменять в методе UpdatePhysics
    private void UpdatePhysics()
    {
        // применяем расчетные значения угловой скорости и крутящего момента колес
        // все 4 колеса имеют "двигатель", т.е. автомобиль полноприводный.
        joint_wheel_fl.AngularVelocity = current_velocity;
        joint_wheel_fr.AngularVelocity = current_velocity;
        joint_wheel_fl.AngularTorque = current_torque;
        joint_wheel_fr.AngularTorque = current_torque;
       
        joint_wheel_rl.AngularVelocity = current_velocity;
        joint_wheel_rr.AngularVelocity = current_velocity;
        joint_wheel_rl.AngularTorque = current_torque;
        joint_wheel_rr.AngularTorque = current_torque;
    }
   
    // добавим методы для управления автомобилем: газ, тормоз, поворот руля и стояночный тормоз
    protected void SetThrottle(float value)
    {
        target_throttle = MathLib.Clamp(value, 0.0f, 1.0f);
    }
   
    protected void SetBrake(float value)
    {
        target_brake = MathLib.Clamp(value, 0.0f, 1.0f);
    }
   
    protected void SetWheelPosition(float value)
    {
        target_wheel = MathLib.Clamp(value, -1.0f, 1.0f);
    }
   
    protected void SetHandBrake(float value)
    {
        target_hand_brake = MathLib.Clamp(value, -1.0f, 1.0f);
    }
   
    // метод смены режима движения, здесь также происходит управление фонарем заднего хода
    protected void SetMoveDirection(MoveDirection value)
    {
        if (current_move_direction == value)
            return;
        current_velocity = 0.0f;
        current_move_direction = value;
        if (reverse_light != null)
            reverse_light.Enabled = current_move_direction == MoveDirection.Reverse;
    }
   
    protected MoveDirection CurrentMoveDirection { get { return current_move_direction; } }
   
    // метод для мгновенного перемещения автомобиля, будет использован для возвращения авто на начальную позицию
    public void Reset(mat4 transform)
    {
        node.WorldTransform = transform;
        node.ObjectBodyRigid.LinearVelocity = vec3.ZERO;
        node.ObjectBodyRigid.AngularVelocity = vec3.ZERO;
        current_velocity = 0.0f;
    }
   
    // получение скорости сразу в км/ч
    public float Speed { get { return CarBodyRigid.LinearVelocity.Length * 3.6f; } }
}