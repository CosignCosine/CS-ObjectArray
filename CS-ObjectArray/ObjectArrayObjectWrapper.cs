using System;
using UnityEngine;

namespace ObjectArray
{
    public class ObjectWrapper
    {
        public enum ObjectType {
            Tree,
            Prop
        }
        public Vector2 size;
        public Vector2 pos;
        public float angle;

        public ObjectType type;

        public TreeInfo tree;
        public PropInfo prop;

        public ObjectWrapper(Vector2 _pos, Vector2 _size, float _angle, PropInfo _prop){
            pos = _pos;
            size = _size;
            angle = _angle;
            prop = _prop;
            type = ObjectType.Prop;
        }

        public ObjectWrapper(Vector2 _pos, Vector2 _size, float _angle, TreeInfo _tree)
        {
            pos = _pos;
            size = _size;
            angle = _angle;
            tree = _tree;
            type = ObjectType.Tree;
        }
    }
}
