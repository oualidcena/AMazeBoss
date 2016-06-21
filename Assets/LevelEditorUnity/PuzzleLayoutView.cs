﻿using System.Collections.Generic;
using UnityEngine;

namespace Assets.LevelEditorUnity
{
    public class EditorWorldObject
    {
        public GameObject GameObject;
        public string Type;

        public EditorWorldObject(string type, GameObject gameObject)
        {
            Type = type;
            GameObject = gameObject;
        }
    }

    [ExecuteInEditMode]
    public class PuzzleLayoutView : MonoBehaviour
    {
        public GameObject Connector;
        public GameObject Node;
        public GameObject Player;

        public Dictionary<TilePos, GameObject> NodeViews = new Dictionary<TilePos, GameObject>();
        public Dictionary<NodeConnection, GameObject> NodeConnectionViews = new Dictionary<NodeConnection, GameObject>();

        private List<GameObject> _previews = new List<GameObject>();
        private NodeConnection _lastPreviewConnection;
        private GameObject _player;

        private PuzzleLayout PuzzleLayout { get { return PuzzleLayout.Instance; } }

        private void LoadLevelStateFromScene()
        {
            PuzzleLayout.Instance.Clear();
            var nodes = gameObject.GetChildren("Node", true);

            foreach (var node in nodes)
            {
                NodeViews.Add(new TilePos(node.transform.localPosition), node);
            }

            var connectors = gameObject.GetChildren("Connector", true);

            foreach (var connector in connectors)
            {
                var start = connector.transform.localPosition;
                var end = start + connector.transform.rotation*Vector3.forward*TilePos.TileLength;
                var nodeConnection = new NodeConnection(new TilePos(start), new TilePos(end));
                PuzzleLayout.Instance.AddNodeConnections(nodeConnection);
                NodeConnectionViews.Add(nodeConnection, connector);
            }
        }

        public void OnEnable()
        {
            LoadLevelStateFromScene();
            PuzzleLayout.NodeAdded += AddNode;
            PuzzleLayout.NodeRemoved += RemoveNode;
            PuzzleLayout.ConnectionAdded += AddNodeConnection;
            PuzzleLayout.ConnectionRemoved += RemoveConnection;
            PuzzleLayout.PlayerAdded += PlayerAdded;
            PuzzleLayout.PlayerRemoved += PlayerRemoved;
        }

        public void OnDisable()
        {
            PuzzleLayout.NodeAdded -= AddNode;
            PuzzleLayout.NodeRemoved -= RemoveNode;
            PuzzleLayout.ConnectionAdded -= AddNodeConnection;
            PuzzleLayout.ConnectionRemoved -= RemoveConnection;
            PuzzleLayout.PlayerAdded -= PlayerAdded;
            PuzzleLayout.PlayerRemoved -= PlayerRemoved;
        }

        private void AddNode(Node node)
        {
            if (!NodeViews.ContainsKey(node.Position))
            {
                var nodeView = CreateNodeView(node);
                NodeViews.Add(node.Position, nodeView);
            }
        }

        private GameObject CreateNodeView(Node node)
        {
            var nodeView = (GameObject) Instantiate(
                Node,
                node.Position.ToV3(),
                Quaternion.identity);
            nodeView.transform.SetParent(transform);
            return nodeView;
        }

        private void RemoveNode(Node node)
        {
            if (NodeViews.ContainsKey(node.Position))
            {
                DestroyImmediate(NodeViews[node.Position]);
                NodeViews.Remove(node.Position);
            }
        }

        public void AddNodeConnection(NodeConnection connection)
        {
            if (!NodeConnectionViews.ContainsKey(connection))
            {
                var connectionView = CreateNodeConnectionView(connection);
                NodeConnectionViews.Add(connection, connectionView);
            }
        }

        private GameObject CreateNodeConnectionView(NodeConnection connection)
        {
            var connectionView = (GameObject) Instantiate(
                Connector,
                connection.Start.ToV3(),
                GetRotationFromConnectionEnd(connection));
            connectionView.transform.SetParent(transform);
            return connectionView;
        }

        private static Quaternion GetRotationFromConnectionEnd(NodeConnection connection)
        {
            return Quaternion.FromToRotation(Vector3.forward, (connection.End - connection.Start).ToV3());
        }

        public void RemoveConnection(NodeConnection connection)
        {
            if (NodeConnectionViews.ContainsKey(connection))
            {
                DestroyImmediate(NodeConnectionViews[connection]);
                NodeConnectionViews.Remove(connection);
            }
        }

        private void PlayerAdded(TilePos position)
        {
            _player = (GameObject) Instantiate(
                Player,
                position.ToV3(),
                Quaternion.identity);

            _player.transform.SetParent(transform);
        }

        private void PlayerRemoved()
        {
            DestroyImmediate(_player);
        }

        public void UpdatePreview(NodeConnection nodeConnection)
        {
            if (!nodeConnection.Equals(_lastPreviewConnection))
            {
                RemovePreview();

                var subConnections = nodeConnection.GetSubdividedConnection();
                foreach (var subConnection in subConnections)
                {
                    AddPreview(CreateNodeConnectionView(subConnection), subConnection.Start, GetRotationFromConnectionEnd(subConnection));
                }
                Debug.Log("Recreated preview");
            }

            _lastPreviewConnection = nodeConnection;
        }

        public void UpdatePreview(EditorWorldObject selectedWorldObject, TilePos position)
        {
            RemovePreview();
            AddPreview(Instantiate(selectedWorldObject.GameObject), position, Quaternion.identity);
        }

        private void AddPreview(GameObject preview, TilePos position, Quaternion rotation)
        {
            preview.name = "Preview";
            preview.transform.localPosition = position.ToV3();
            preview.transform.rotation = rotation;
            _previews.Add(preview);
        }

        public void RemovePreview()
        {
            if (_previews != null)
            {
                foreach (var preview in _previews)
                {
                    DestroyImmediate(preview);
                }
                _previews = new List<GameObject>();
            }
        }
    }
}