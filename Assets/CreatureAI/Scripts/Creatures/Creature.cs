using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Creature{
    public enum CreatureStatus
    {
        PATROL = 0, // 기본
        PURSUIT = 1, // 추적
        ALERTED = 2, // 의심
        AVOIDING = 3,
    }

    public class CreatureAction
    {
        protected Creature creature;
        public virtual void Play() {}
    }

    public class CreaturePatrolAction : CreatureAction
    {
        public CreaturePatrolAction(Creature creature)
        {
            this.creature = creature;
        }

        public override void Play()
        {
            creature.StartCoroutine(creature.PatrolAction());
        }
    }

    public class CreaturePursuitAction : CreatureAction
    {
        public CreaturePursuitAction(Creature creature)
        {
            this.creature = creature;
        }

        public override void Play()
        {
            creature.StartCoroutine(creature.PursuitAction());
        }
    }

    public class CreatureAlertedAction : CreatureAction
    {
        public CreatureAlertedAction(Creature creature)
        {
            this.creature = creature;
        }

        public override void Play()
        {
            creature.StartCoroutine(creature.AlertedAction());
        }
    }

    public class Creature : Actor
    {
        [SerializeField] protected bool debugMode = true;

        private Detector detector;

        private PathLineRenderer pathLineRenderer;
        protected int[,] map;
        protected Vector3Int mapOffset;
        private Vector3 deltaPosition = new Vector3();
        private Node startNode = new Node(true);
        private Node endNode = new Node(true);
        private float moveFrame = 0.05f;

        protected PathFinder pathFinder;

        protected Vector3 targetPosition;
        protected List<Node> path;

        private bool isChasing = false;

        protected List<CreatureAction> actions;

        protected CreatureStatus status;
        private Coroutine alertedCounterCoroutine;

        protected int maxSpeed;
        protected int minSpeed;

        private void InitActions()
        {
            actions = new List<CreatureAction>()
            {
                new CreaturePatrolAction(this),
                new CreaturePursuitAction(this),
                new CreatureAlertedAction(this)
            };

        }

        protected void Start()
        {
            status = CreatureStatus.PATROL;
            map = CreatureManager.Instance.GetMap();
            mapOffset = CreatureManager.Instance.GetMapOffset();
            pathLineRenderer = GetComponent<PathLineRenderer>();
            detector = GetComponent<Detector>();
            detector.SetTargetMask(LayerMask.GetMask("Player"));
            InitActions();
        }

        public virtual IEnumerator PatrolAction()
        {
            if (debugMode)
            {
                Debug.Log(gameObject.name + " | " + this.name + " : Patrol...");
            }

            speed = minSpeed;
            DetectPlayer();
            yield return new WaitForSeconds(0.1f);
            actions[(int)status].Play();
        }

        public virtual IEnumerator PursuitAction()
        {
            if (debugMode)
            {
                Debug.Log(gameObject.name + " | " + this.name + " : Pursuit...");
            }

            speed = maxSpeed;
            DetectPlayer();
            yield return new WaitForSeconds(0.1f);
            GetPathToPosition(targetPosition);
            actions[(int)status].Play();
        }

        public virtual IEnumerator AlertedAction()
        {
            if (debugMode)
            {
                Debug.Log(gameObject.name + " | " + this.name + " : Alerted...");
            }

            speed = minSpeed;
            DetectPlayer();
            detector.setLookingAngle(detector.getLookingAngle() + 10f);
            yield return new WaitForSeconds(0.1f);
            SetRandomPath();
            actions[(int)status].Play();
        }

        public IEnumerator AlertedCounter()
        {
            yield return new WaitForSeconds(10f);
            if (status.Equals(CreatureStatus.ALERTED))
                status = CreatureStatus.PATROL;
            alertedCounterCoroutine = null;
        }

        protected void DetectPlayer()
        {
            if (debugMode)
            {
                Debug.Log(gameObject.name + " | " + this.name + " : Detecting Player...");
            }

            List<Collider2D> detectedPlayerCollider = detector.DetectByView();
            if (detectedPlayerCollider.Count > 0)
            {
                status = CreatureStatus.PURSUIT;
                Vector3 detectedPlayerPosition = detectedPlayerCollider[0].transform.position;
                targetPosition = detectedPlayerPosition;
            } else if (status.Equals(CreatureStatus.PURSUIT) && !isChasing)
            { 
                status = CreatureStatus.ALERTED;
                if (alertedCounterCoroutine != null)
                {
                    StopCoroutine(alertedCounterCoroutine);
                    alertedCounterCoroutine = null;
                }
                alertedCounterCoroutine = StartCoroutine(AlertedCounter());
            }
        }

        protected void GetPathToPosition(Vector3 targetPosition)
        {
            path = FindPath(targetPosition.x, targetPosition.y);
        }


        protected virtual List<Node> FindPath(float x, float y)
        {
            startNode.SetPosition(transform.position.x, transform.position.y);
            endNode.SetPosition(x, y);
            
            try
            {
                List<Node> path = pathFinder.FindPath(startNode, endNode);
                if (debugMode)
                {
                    pathLineRenderer.SetPoints(path);
                }
                return path;
            }
            catch (Exception e)
            {
                print(e.Message);
                return null;
            }
        }

        protected virtual void SetRandomPath()
        {
            path = pathFinder.GetRandomPath(10, deltaPosition, Vector3ToVector3Int(transform.position));
        }

        protected IEnumerator MoveOnPath()
        {
            while(true)
            {
                if (debugMode)
                {
                    Debug.Log(gameObject.name + " | " + this.name + " : is Moving");
                    lastStatus = status;
                }

                yield return new WaitForSeconds(0.01f);

                Node node;

                if (path == null || path.Count == 0)
                {
                    isChasing = false;
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                node = path[0];
                if (node.X == transform.position.x && node.Y == transform.position.y)
                {
                    if (path.Count == 1)
                    {
                        isChasing = false;
                        yield return new WaitForSeconds(1f);
                        continue;
                    }
                        
                    node = path[1];
                    path.RemoveAt(1);
                }
                path.RemoveAt(0);
                

                isChasing = true;

                float deltaX = node.X - transform.position.x;
                float deltaY = node.Y - transform.position.y;
                deltaPosition.Set(deltaX, deltaY, 0);
                deltaX = Math.Abs(deltaX);
                deltaY = Math.Abs(deltaY);

                detector.SetLookingDirection(deltaPosition);

                deltaPosition.Normalize();

                while (deltaX > 0 || deltaY > 0)
                {
                    deltaX -= Math.Abs(deltaPosition.x * speed * moveFrame);
                    deltaY -= Math.Abs(deltaPosition.y * speed * moveFrame);
                    transform.position += (deltaPosition * speed * moveFrame);
                    yield return new WaitForSeconds(moveFrame);
                }
                transform.position = new Vector3(node.X, node.Y);
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (debugMode)
                {
                    Debug.Log(gameObject.name + " | " + this.name + " : Kill Player...");
                }

                Destroy(collision.gameObject);
            }
        }

        protected Vector3Int Vector3ToVector3Int(Vector3 vector)
        {
            return new Vector3Int(
                Mathf.RoundToInt(vector.x),
                Mathf.RoundToInt(vector.y),
                Mathf.RoundToInt(vector.z)
            );
        }

        CreatureStatus lastStatus = CreatureStatus.PATROL;

        protected void Update()
        {
            if (debugMode)
            {
                if (status != lastStatus)
                {
                    Debug.Log(gameObject.name + " | " + this.name + " : updated status to " + status.ToString());
                    lastStatus = status;
                }
            }
        }
    }
}

