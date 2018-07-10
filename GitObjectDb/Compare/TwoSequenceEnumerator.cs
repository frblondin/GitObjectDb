using GitObjectDb.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Compare
{
#pragma warning disable SA1600 // Elements must be documented
    internal sealed class TwoSequenceEnumerator<T> : IDisposable
        where T : class, IMetadataObject
    {
        readonly IEnumerator<T> _leftEnumerator;
        readonly IEnumerator<T> _rightEnumerator;
        bool _leftCompleted;
        bool _rightCompleted;

        public TwoSequenceEnumerator(IEnumerable<T> leftElements, IEnumerable<T> rightElements)
        {
            if (leftElements == null)
            {
                throw new ArgumentNullException(nameof(leftElements));
            }
            if (rightElements == null)
            {
                throw new ArgumentNullException(nameof(rightElements));
            }

            _leftEnumerator = leftElements.OrderBy(v => v.Id).GetEnumerator();
            _rightEnumerator = rightElements.OrderBy(v => v.Id).GetEnumerator();
            MoveNextLeft();
            MoveNextRight();
        }

        public bool BothCompleted => _leftCompleted && _rightCompleted;

        public T Left => !_leftCompleted ? _leftEnumerator.Current : default;

        public T Right => !_rightCompleted ? _rightEnumerator.Current : default;

        public (T, T) Current => (Left, Right);

        public bool NodeIsStillThere =>
            Right != null && Left != null && Left.Id == Right.Id;

        public bool NodeHasBeenAdded =>
            (Left == null && Right != null) ||
            (Left != null && Right != null && Left.Id.CompareTo(Right.Id) > 0);

        public bool NodeHasBeenRemoved =>
            (Left != null && Right == null) ||
            (Left != null && Right != null && Left.Id.CompareTo(Right.Id) < 0);

        public void MoveNextLeft() => _leftCompleted = !_leftEnumerator.MoveNext();

        public void MoveNextRight() => _rightCompleted = !_rightEnumerator.MoveNext();

        public void Dispose()
        {
            _leftEnumerator.Dispose();
            _rightEnumerator.Dispose();
        }
    }
#pragma warning restore SA1600 // Elements must be documented
}
