//namespace Pong.Ball;
/*
This file is important because it handles aspects of the ball to make sure it functions properly. This includes when the balls scores, the ball velocity, and the serves
The method serve is important in this file. It serves the ball to the correct side at a random angle.
The method reset is important. It resets the ball's position.
The method DestroyBall is important. This makes the ball "dissappear" once it goes off the screen into the goal.
*/
using Pong.Ball;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityUtils;

using Pong;
using Pong.GamePlayer;
using Pong.Physics;
using static Pong.GameHelpers;
using static Pong.GameCache;

namespace Pong.Ball {
    //* After scoring, it goes: DestroyBall() -> [tiny delay] -> Reset() -> [small delay] -> Serve()
    public partial class PongBall {
        public readonly ControlledGameObject<PongBallController> ballSprite; // it won't actually be destroyed; it will just vanish and look like it was destroyed
        private readonly Stack<(float, bool)> serveAngles = new Stack<(float, bool)>(); // float is in radians, and the int is the attackerDesire

        // Player on the offensive
        private Player attacker; // "lastTouchedBy"; the initial trajectory will also set this as the player opposite to where it is traveling
        private bool attackerDesire;

        // Listeners
        private readonly Action OnScore;
        private readonly Action OnRebound;

        public PongBall(GameObject sprite) {
            OnScore = () => {
                //* Attacker Scored Goal

                DestroyBall();

                attacker.ScorePoint();

                //TODO: [tiny delay]
                Reset();
                //TODO: [small delay]
                Serve();
            };

            OnRebound = () => {
                //* Ball was Rebounded by the defender
                ballSprite.controller.ResetBallState();
                SwapAttacker();
            };

            // add + initialize controller
            PongBallController controller = sprite.AddComponent<PongBallController>();
            controller.Initialize(OnScore, OnRebound);

            // collision detection
            RectangularBodyFrame bodyFrame = sprite.AddComponent<RectangularBodyFrame>();

            // wrap it up
            ballSprite = new ControlledGameObject<PongBallController>(sprite, controller);
        }

        public static PongBall FromPrefab(GameObject prefab) {
            GameObject sprite = GameManager.Instantiate(prefab, GetStartLocalPosition(), Quaternion.identity);

            PongBall pongBall = new PongBall(sprite);
            pongBall.SetLocalScaleFromVPY(GameConstants.BALL_SCALE_Y);

            return pongBall;
        }

        public void Initialize(Player server) {
            // First, reset the ball (just in case)
            Reset();

            // Handle which server this is
            bool serverIsToRight = server.playerSprite.transform.localPosition.x > GetStartLocalPosition().x;

            // initialize desire
            attackerDesire = serverIsToRight; // serverIsToRight => serverIsToRight = BallGoal.LEFT = true

            // either 0 or 1, depending on whether it is even or odd respectively
            // if first server (even, index 0) is to the left, the remaining odd servers will be to the right and therefore have the ball traveling left on serve
            // if first server is to the right, all the rest of the even servers on the right will have the ball traveling left on serve. the remaining odd servers will be to the left, and the ball 
            uint playerFactor = (uint)(serverIsToRight ? 0 : 1);

            // initialize serveAngles
            uint maxRounds = (WIN_SCORE) + (WIN_SCORE - 1);
            for (uint i = 0; i < maxRounds; ++i) {
                float angle = UnityEngine.Random.Range(-BALL_SERVE_MAX_ANGLE, BALL_SERVE_MAX_ANGLE); // base; works for if server is on left
                bool desire;

                // (i % 2 == 0) => first server is to the right => the right server is on even rounds to serve left
                // (i % 2 == 1) => first server is to the left => the right server is on odd rounds to serve left
                if (i % 2 == playerFactor) { // if odd/even, add PI so that it goes on the left side 
                    //* Player on the Right's turn to Serve
                    angle += Mathf.PI;
                    desire = BallGoal.LEFT;
                } else {
                    //* Player on the Left's turn to Serve
                    desire = BallGoal.RIGHT;
                }

                //Debug.Log(angle);
                serveAngles.Push((angle, desire));
                
                //TODO: debug
                /*if (i == 10) {
                    break;
                }*/
            }
            
            // the Player serving is the one on the offensive
            SetAttacker(server);
        }

        // serve the ball
/// <summary>
/// It serves the ball to the correct side at a random angle.
/// </summary>
/// <returns> void, doesnt return anything
        public void Serve() {
            (float angle, bool serverDesire) = serveAngles.Pop();
            float speed = BALL_SPEED_VP; // in terms of viewport x percentage

            bool otherPlayerServingInstead = attackerDesire != serverDesire;
            if (otherPlayerServingInstead) {
                SwapAttacker();
            }

            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); // unit vector
            Vector2 viewportVelocity = speed * direction;

            ballSprite.controller.ViewportVelocity = viewportVelocity; // set velocity
            ballSprite.controller.BeginTrajectory(); // start the timer for y'(t)
        }

        public void Update() {
            //TODO: feed to players?
        }
/// <summary>
/// This makes the ball "dissappear" once it goes off the screen into the goal.
/// </summary>
/// <returns> void, doesnt return anything
        public void DestroyBall() {
            ballSprite.gameObj.SetActive(false);
            ballSprite.controller.HaltTrajectory(); // stop the ball from going off the screen
        }
/// <summary>
/// It resets the ball's position.
/// </summary>
/// <returns> void, doesnt return anything
        public void Reset() {
            // set position to start position
            ballSprite.transform.localPosition = GetStartLocalPosition();

            // activate
            ballSprite.gameObj.SetActive(true);
        }

        private void SetAttacker(Player atkr) {
            attacker = atkr;

            // Now that the attacker has been set, the ball will be headed towards the "rebounder", or in other words, the other player
            ballSprite.controller.Rebounder = attacker.Opponent.AsRebounder();
        }

        private void SwapAttacker() {
            SetAttacker(attacker.Opponent);
            attackerDesire = !attackerDesire;
        }

        public void SetLocalScaleFromVPY(float viewportY) {
            Vector3 bgScale = BG_TRANSFORM.localScale;

            ballSprite.transform.localScale = new Vector3(
                viewportY * bgScale.y, // square
                viewportY * bgScale.y, // square
                ballSprite.transform.localScale.z
            );

            //Debug.Log("LocalScale: " + sprite.transform.localScale);
        }

        public static Vector3 GetStartLocalPosition() {
            return ToLocal(GameConstants.BALL_START_POSITION);
        }
    }
}