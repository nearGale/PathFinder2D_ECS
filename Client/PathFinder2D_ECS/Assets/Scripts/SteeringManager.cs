﻿using System;
using UnityEngine;

namespace Application
{
    public enum SteeringBehavior
    {
        Seek,
        Flee,
        Wander,
    }

    public interface IEntity
    {
        Vector2 GetVelocity();
        float GetMaxVelocity();
        Vector2 GetPosition();
        float GetMass();
        void SetPosition(Vector2 pos);
        void SetVelocity(Vector2 velocity);
        
        float GetSlowingRadius();
        GameObject GetHostGameObject();
        float GetMaxSeeAhead();
        float GetMaxAvoidForce();
    }
    public class SteeringManager
    {
        private float m_ForceSteer_Max = 1f;


        public IEntity host
        {
            get; private set;
        }
        public Vector2 Steering {
            get; private set;
        }
        public SteeringManager(IEntity host)
        {
            this.host = host;
            Steering = Vector2.zero;
        }

        public void Update()
        {
            Vector2 v = host.GetVelocity();
            Vector2 pos = host.GetPosition();
            var tempSteering = Steering;
            VectorHandler.Truncate(ref tempSteering, m_ForceSteer_Max);
            Steering = tempSteering;
            Steering *= 1 / host.GetMass();
 
            v = v + Steering;
            VectorHandler.Truncate(ref v, host.GetMaxVelocity());
 
            host.SetVelocity(v);
            host.SetPosition(pos + v);
        }

        /// <summary>
        /// 向目标位置行进时，计算本体的转向力，增加给 Steering。
        /// 在减速距离内，转向力与目标的距离成正比。
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <param name="slowingRadius">距离目标开始减速的距离</param>
        public void Seek(Vector2 target)
        {
            Steering += DoSeek(target, host.GetSlowingRadius());
        }

        /// <summary>
        /// 向目标位置行进时，计算转向力。
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <param name="slowingRadius">距离目标开始减速的距离</param>
        /// <returns></returns>
        private Vector2 DoSeek(Vector2 target, float slowingRadius = 1f)
        {
            Vector2 force ;
            float distance;
 
           var desired = target - host.GetPosition();
 
            distance = desired.magnitude;
            desired.Normalize();
 
            if (distance <= slowingRadius) {
                desired *= host.GetMaxVelocity() * distance/slowingRadius;
            } else {
                desired *= host.GetMaxVelocity();
            }
 
            force = desired - host.GetVelocity();
 
            return force;
        }

        /// <summary>
        /// 向目标位置追赶，计算转向力。
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <param name="velocity">目标速度</param>
        /// <returns></returns>
        Vector2  DoPursuit(Vector2 target, Vector2 velocity){
            var distance = target - host.GetPosition();
            var T = distance.magnitude / host.GetMaxVelocity();
            var futurePosition = target + velocity * T;
            return  DoSeek(futurePosition);

        }
        public void Pursuit(IEntity target){
            Steering += DoPursuit(target.GetPosition(),target.GetVelocity());
        }
        /// <summary>
        /// 检测运动方向上的障碍物，添加避免碰撞的力。
        /// </summary>
        /// <returns></returns>
        public void CollisionAvoidance()
        {
            Steering += DoCollisionAvoidance();
        }

        Vector2 DoCollisionAvoidance()
        {
            Physics2D.queriesStartInColliders = false;

            var dir = host.GetVelocity().normalized;
            var ahead =  dir * host.GetMaxSeeAhead();
            var ahead2 =  dir * host.GetMaxSeeAhead() *0.5f;
            // Bit shift the index of the layer to get a bit mask
            int layerMask = 1 << (int)ELayer.Entity | 1<< (int)ELayer.Wall;

            // This would cast rays only against colliders in layer Entity and layer Wall.
            
            host.GetHostGameObject().GetComponent<BoxCollider2D>();
            bool hit = false;
            RaycastHit2D hit2D = Physics2D.Raycast(host.GetPosition(), dir, ahead2.magnitude,layerMask);
            if (hit2D.transform != null)
            {
                hit = true;
            }
            
            if (!hit)
            {
                hit2D = Physics2D.Raycast(host.GetPosition(), dir, ahead.magnitude,layerMask);
            }
            
            if(hit)
            {
                if (hit2D.transform != null)
                {
                    Debug.DrawRay(host.GetPosition(), dir *ahead.magnitude *2f, Color.red);
                    Debug.Log("Did Hit" + hit2D.transform.name + "at: " + hit2D.point);
                    Vector2 avoidance = Vector2.zero;
                    var collider = hit2D.collider;
                    if (collider is BoxCollider2D)
                    {
                        Vector2 center = new Vector2(hit2D.collider.transform.position.x, collider.transform.position.y);
                        var obstacle = new Obstacle(center, collider.transform.localScale.x, collider.transform.localScale.y);

                        if (collider.transform.localScale.x == collider.transform.localScale.y)
                        {
                            avoidance = host.GetPosition() - hit2D.point;
                            //avoidance = hit2D.point - center;
                        }
                        else if (collider.transform.localScale.y > collider.transform.localScale.x)
                        {
                            // Vertical
                            avoidance = host.GetPosition() - new Vector2(center.x, host.GetPosition().y);
                        }
                        else
                        {
                            // horizontal
                            avoidance = host.GetPosition() - new Vector2(host.GetPosition().x, center.y);
                        }



                    }
                    else if (hit2D.collider is CircleCollider2D)
                    {
                        Vector2 center = new Vector2(hit2D.collider.transform.position.x, collider.transform.position.y);
                        var obstacle = new Obstacle(center, collider.transform.localScale.x);
                        avoidance = hit2D.point - center;
                    }

                    return avoidance.normalized * host.GetMaxAvoidForce() -0.8f *host.GetVelocity();
                }
            }
            else
            {
                Debug.DrawRay(host.GetPosition(), dir, Color.green);
            }

            return Vector2.zero;
        }
        /// <summary>
        /// 向目标位置相反方向逃离，将力增加给 Steering。
        /// </summary>
        /// <param name="target">目标位置</param>
        public void Flee(Vector2 target)
        {
            Steering += DoFlee(target);
        }

        /// <summary>
        /// 向目标位置相反方向逃离时，计算转向力。
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <returns></returns>
        private Vector2 DoFlee(Vector2 target)
        {
            var desired = host.GetPosition() - target;
            desired.Normalize();
            desired *= host.GetMaxVelocity();

            Vector2 force = desired;
            return force;
        }
        
        /// <summary>
        /// 距离目的地 开始减速的距离
        /// </summary>
        private float m_SlowingRadius;

        /// <summary>
        /// 漫步圆的距离
        /// </summary>
        private float m_CircleDistance = 0.3f;

        /// <summary>
        /// 漫步圆的大小
        /// </summary>
        private float m_CircleRadius = 0.1f;

        /// <summary>
        /// 本次 wander 的角度
        /// </summary>
        private float m_CurrentWanderAngle;

        /// <summary>
        /// wander 转向最大角度
        /// </summary>
        private float m_AngleChange_Max = 50;

        /// <summary>
        /// 游荡行为，将力赋值给 Steering。
        /// </summary>
        public void Wander()
        {
            if (host == null)
                return;
            
            Steering = DoWander(host.GetVelocity());
        }

        /// <summary>
        /// 游荡行为时，计算转向力。
        /// </summary>
        /// <param name="moveDir">初始方向</param>
        /// <returns></returns>
        private Vector2 DoWander(Vector2 moveDir)
        {
            Vector2 circleCenter = moveDir.normalized * m_CircleDistance;

            Vector2 displacement = Vector2.down * m_CircleRadius;

            // 通过修改当前角度，随机改变向量的方向，m_CurrentWanderAngle 初始为 0，默认向上
            SetAngle(ref displacement, m_CurrentWanderAngle);

            m_CurrentWanderAngle += (UnityEngine.Random.Range(0f, 1f) * m_AngleChange_Max) - (m_AngleChange_Max * 0.5f);

            Vector2 wanderForce = circleCenter + displacement;

            return wanderForce;
        }

        private void SetAngle(ref Vector2 vector, float value)
        {
            float len = vector.magnitude;
            vector.x = Mathf.Cos(value) * len;
            vector.y = Mathf.Sin(value) * len;
        }

    }
}
