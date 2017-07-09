using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalendarBackendLib
{
    public enum ChangeState
    {
        Created,
        Changed,
        Unchanged
    }

    public class ChangedObject<T>
    {
        public readonly T Obj;
        public readonly ChangeState ChangeState;

        public ChangedObject(T obj, ChangeState state)
        {
            Obj = obj;
            ChangeState = state;
        }

        public override string ToString()
        {
            return ChangeState.ToString() + " " + Obj.ToString();
        }
    }

    public class ChangedObject : ChangedObject<string>
    {
        public ChangedObject(string obj, ChangeState state)
            : base(obj, state)
        { }
    }

    public static class ChangedObjectExtensions
    {
        public static int CountCreated<T>(this IEnumerable<ChangedObject<T>> list)
        {
            return list.Count(o => o.ChangeState == ChangeState.Created);
        }

        public static int CountChanged<T>(this IEnumerable<ChangedObject<T>> list)
        {
            return list.Count(o => o.ChangeState == ChangeState.Changed);
        }

        public static int CountUnchanged<T>(this IEnumerable<ChangedObject<T>> list)
        {
            return list.Count(o => o.ChangeState == ChangeState.Unchanged);
        }

        public static string CountChangeStateAndReturnAsHumanReadableString<T>(this IEnumerable<ChangedObject<T>> list)
        {
            return string.Format("Created {0}, Changed {1}, Unchanged {2}", list.CountCreated(), list.CountChanged(), list.CountUnchanged());
        }

        public static IEnumerable<T> OnlyCreated<T>(this IEnumerable<ChangedObject<T>> list)
        {
            return list
                .Where(o => o.ChangeState == ChangeState.Created)
                .Select(o => o.Obj);
        }
        public static IEnumerable<T> OnlyChanged<T>(this IEnumerable<ChangedObject<T>> list)
        {
            return list
                .Where(o => o.ChangeState == ChangeState.Changed)
                .Select(o => o.Obj);
        }
        public static IEnumerable<T> OnlyUnchanged<T>(this IEnumerable<ChangedObject<T>> list)
        {
            return list
                .Where(o => o.ChangeState == ChangeState.Unchanged)
                .Select(o => o.Obj);
        }
    }
}
