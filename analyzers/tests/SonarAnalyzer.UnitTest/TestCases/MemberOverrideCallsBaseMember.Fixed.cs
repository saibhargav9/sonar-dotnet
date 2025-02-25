﻿using System;
using System.Collections.Generic;

namespace Tests.Diagnostics
{
    class Base
    {
        public virtual int MyProperty { get; set; }
        public virtual int MyProperty1 { get; set; }
        public virtual int MyProperty2 { get; }
        public virtual int MyProperty3 { get; }
        public virtual int MyProperty4 { get; set; }

        public virtual void Method(int[] numbers)
        {
        }
        public virtual int Method2(int[] numbers)
        {
            return 1;
        }
        public virtual int Method3(int[] numbers)
        {
            return 1;
        }
        public virtual int Method4(int[] numbers)
        {
            return 1;
        }
        public virtual void Method(string s1, string s2)
        {
        }
        public virtual void Method2(string s1, string s2)
        {
        }
        public virtual void Method(int i, int[] numbers)
        {
        }
        public virtual void Method3(string s1, string s2)
        {
        }
        public virtual void Method4(string s1, params string[] s2)
        {
        }
        public virtual void Method5(string s1, string s2)
        {
        }

        public virtual int Method6(string s1, string s2)
        {
            return 1;
        }

        public virtual void Method8(string s1, string s2 = null)
        {
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    class Derived : Base
    {
        public override int MyProperty3 => 42;

        public Base bbb;
        public override int Method4(int[] numbers2) => base.Method4(numbers2);
        public override void Method(string s1, string s2)
        {
            base.Method(s2, s1);
        }
        public override void Method2(string s1, string s2)
        {
            bbb.Method2(s1, s2);
        }
        public override sealed void Method(int i, int[] numbers)
        {
            base.Method(i, numbers);
        }
        public override void Method3(string s1, string s2)
        {
            base.Method(s1, s2);
        }
        public override void Method4(string s1, params string[] s2)
        {
            base.Method4(s1);
        }
        public override void Method5(string s1, string s2)
        {
            base.Method5(s1, null);
        }
        public override int Method6(string s1, string s2)
        {
            return base.NonExisteng(s1, s2); // Error [CS0117] 'Base' does not contain a definition for 'NonExisteng'
        }
        public override void Method7(string s1, string s2) // Error [CS0115] no suitable method found to override
        {
            return base.Method7(s1, s2); // Error [CS0117] 'Base' does not contain a definition for 'Method7'
        }
        public override void Method8(string s1, string s2)
        {
            base.Method8(s1, s2);
        }
    }

    public class A
    {
        public virtual void Foo1(int a)
        {
        }

        public virtual void Foo2(int a = 42)
        {
        }

        public virtual void Foo3(int a)
        {
        }
        public virtual void Foo4(int a = 42)
        {
        }
    }

    public class B : A
    {
        public override void Foo1(int a = 1)
        {
            base.Foo1(a);
        }

        public override void Foo2(int a = 1)
        {
            base.Foo2(a);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        public override void Foo3(int a)
        {
            base.Foo3(a);
        }

        public virtual void Foo4(int a)
        {
            base.Foo4(a);
        }
    }

    public class MyBase
    {
        public virtual int MyProperty1 { get; }
        public virtual int MyProperty2 { get; }
    }

    public class MyDerived : MyBase
    {
        private MyBase instance;

        public override int MyProperty2
        {
            get
            {
                return instance.MyProperty2;
            }
        }
    }

    public class MyCustomAttribute : Attribute
    {
        public string Text { get; set; }
    }

    public class AnotherCustomAttribute : Attribute
    {
        public string Text { get; set; }
    }

    class MyAttributesTestCase
    {
        [MyCustomAttribute]
        public virtual int PropertyWithSameInheritedAttributeOnAllLevels => 1;
        [MyCustomAttribute]
        public virtual int PropertyWithInheritedAttributeOnFirstLevel => 1;
        public virtual int PropertyWithInheritedAttributeOnSecondLevel => 1;
        public virtual int PropertyWithInheritedAttributeOnThirdLevel => 1;
        [MyCustomAttribute]
        public virtual int PropertyWithSameInheritedAttributeOnFirstAndSecondLevels => 1;
        [MyCustomAttribute]
        public virtual int PropertyWithSameInheritedAttributeOnFirstAndThirdLevels => 1;
        [MyCustomAttribute]
        public virtual int PropertyWithDifferentInheritedAttributeOnFirstAndSecondLevels => 1;

        [MyCustomAttribute]
        public virtual int MethodWithSameInheritedAttributeOnAllLevels()
        {
            return 1;
        }
        [MyCustomAttribute]
        public virtual int MethodWithInheritedAttributeOnFirstLevel()
        {
            return 1;
        }
        public virtual int MethodWithInheritedAttributeOnSecondLevel()
        {
            return 1;
        }
        public virtual int MethodWithInheritedAttributeOnThirdLevel()
        {
            return 1;
        }
        [MyCustomAttribute]
        public virtual int MethodWithSameInheritedAttributeOnFirstAndSecondLevels()
        {
            return 1;
        }
        [MyCustomAttribute]
        public virtual int MethodWithSameInheritedAttributeOnFirstAndThirdLevels()
        {
            return 1;
        }
        [MyCustomAttribute]
        public virtual int MethodWithDifferentInheritedAttributeOnFirstAndSecondLevels()
        {
            return 1;
        }
    }

    class MySubAttributesTestCase : MyAttributesTestCase
    {
        [MyCustomAttribute]
        public override int PropertyWithSameInheritedAttributeOnAllLevels => base.PropertyWithSameInheritedAttributeOnAllLevels;
        public override int PropertyWithInheritedAttributeOnFirstLevel => base.PropertyWithInheritedAttributeOnFirstLevel;
        [MyCustomAttribute]
        public override int PropertyWithInheritedAttributeOnSecondLevel => base.PropertyWithInheritedAttributeOnSecondLevel;
        [MyCustomAttribute]
        public override int PropertyWithSameInheritedAttributeOnFirstAndSecondLevels => base.PropertyWithSameInheritedAttributeOnFirstAndSecondLevels;
        public override int PropertyWithSameInheritedAttributeOnFirstAndThirdLevels => base.PropertyWithSameInheritedAttributeOnFirstAndThirdLevels;
        [AnotherCustomAttribute]
        public override int PropertyWithDifferentInheritedAttributeOnFirstAndSecondLevels => base.PropertyWithDifferentInheritedAttributeOnFirstAndSecondLevels;

        [MyCustomAttribute]
        public override int MethodWithSameInheritedAttributeOnAllLevels() => base.MethodWithSameInheritedAttributeOnAllLevels();
        public override int MethodWithInheritedAttributeOnFirstLevel() => base.MethodWithInheritedAttributeOnFirstLevel();
        [MyCustomAttribute]
        public override int MethodWithInheritedAttributeOnSecondLevel() => base.MethodWithInheritedAttributeOnSecondLevel();
        [MyCustomAttribute]
        public override int MethodWithSameInheritedAttributeOnFirstAndSecondLevels() => base.MethodWithSameInheritedAttributeOnFirstAndSecondLevels();
        public override int MethodWithSameInheritedAttributeOnFirstAndThirdLevels() => base.MethodWithSameInheritedAttributeOnFirstAndThirdLevels();
        [AnotherCustomAttribute]
        public override int MethodWithDifferentInheritedAttributeOnFirstAndSecondLevels() => base.MethodWithDifferentInheritedAttributeOnFirstAndSecondLevels();
    }

    class MySubSubAttributesTestCase : MySubAttributesTestCase
    {
        [MyCustomAttribute]
        public override int PropertyWithSameInheritedAttributeOnAllLevels => base.PropertyWithSameInheritedAttributeOnAllLevels;
        public override int PropertyWithInheritedAttributeOnFirstLevel => base.PropertyWithInheritedAttributeOnFirstLevel;
        public override int PropertyWithInheritedAttributeOnSecondLevel => base.PropertyWithInheritedAttributeOnSecondLevel;
        [MyCustomAttribute]
        public override int PropertyWithInheritedAttributeOnThirdLevel => base.PropertyWithInheritedAttributeOnThirdLevel;
        public override int PropertyWithSameInheritedAttributeOnFirstAndSecondLevels => base.PropertyWithSameInheritedAttributeOnFirstAndSecondLevels;
        [MyCustomAttribute]
        public override int PropertyWithSameInheritedAttributeOnFirstAndThirdLevels => base.PropertyWithSameInheritedAttributeOnFirstAndThirdLevels;

        [MyCustomAttribute]
        public override int MethodWithSameInheritedAttributeOnAllLevels() => base.MethodWithSameInheritedAttributeOnAllLevels();
        public override int MethodWithInheritedAttributeOnFirstLevel() => base.MethodWithInheritedAttributeOnFirstLevel();
        public override int MethodWithInheritedAttributeOnSecondLevel() => base.MethodWithInheritedAttributeOnSecondLevel();
        [MyCustomAttribute]
        public override int MethodWithInheritedAttributeOnThirdLevel() => base.MethodWithInheritedAttributeOnThirdLevel();
        public override int MethodWithSameInheritedAttributeOnFirstAndSecondLevels() => base.MethodWithSameInheritedAttributeOnFirstAndSecondLevels();
        [MyCustomAttribute]
        public override int MethodWithSameInheritedAttributeOnFirstAndThirdLevels() => base.MethodWithSameInheritedAttributeOnFirstAndThirdLevels();
    }
}
