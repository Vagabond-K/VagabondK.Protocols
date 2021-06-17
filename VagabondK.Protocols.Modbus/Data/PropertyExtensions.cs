using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VagabondK.Protocols.Modbus.Data
{
    /// <summary>
    /// 속성 관련 확장 메서드 모음
    /// </summary>
    static class PropertyExtensions
    {
        /// <summary>
        /// 속성 가져오기. null일 경우 초기화 동작의 결과를 반환. Lazy와 비슷함.
        /// </summary>
        /// <typeparam name="TProperty">속성 형식</typeparam>
        /// <param name="obj">속성을 포함한 객체</param>
        /// <param name="target">속성 저장 멤버</param>
        /// <param name="factory">속성 초기화 동작</param>
        /// <returns>속성 값</returns>
        public static TProperty Get<TProperty>(this object obj, ref TProperty target, Func<TProperty> factory)
        {
            if (target == null && factory != null)
                target = factory.Invoke();

            return target;
        }

        /// <summary>
        /// 속성 설정
        /// </summary>
        /// <typeparam name="TProperty">속성 형식</typeparam>
        /// <param name="obj">속성을 포함한 객체</param>
        /// <param name="target">속성 저장 멤버</param>
        /// <param name="value">설정 값</param>
        /// <returns>값 변경 여부</returns>
        public static bool Set<TProperty>(this object obj, ref TProperty target, TProperty value)
        {
            if (!EqualityComparer<TProperty>.Default.Equals(target, value))
            {
                target = value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 속성 설정
        /// </summary>
        /// <typeparam name="TProperty">속성 형식</typeparam>
        /// <param name="obj">속성을 포함한 객체</param>
        /// <param name="target">속성 저장 멤버</param>
        /// <param name="value">설정 값</param>
        /// <param name="propertyChangedEvent">PropertyChanged 이벤트</param>
        /// <param name="propertyName">속성 명</param>
        /// <returns>값 변경 여부</returns>
        public static bool Set<TProperty>(this INotifyPropertyChanged obj, ref TProperty target, TProperty value, PropertyChangedEventHandler propertyChangedEvent, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<TProperty>.Default.Equals(target, value))
            {
                target = value;
                propertyChangedEvent?.Invoke(obj, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        /// <summary>
        /// 속성 설정
        /// </summary>
        /// <typeparam name="TProperty">속성 형식</typeparam>
        /// <param name="obj">속성을 포함한 객체</param>
        /// <param name="target">속성 저장 멤버</param>
        /// <param name="value">설정 값</param>
        /// <param name="propertyChangingEvent">PropertyChanging 이벤트</param>
        /// <param name="propertyName">속성 명</param>
        /// <returns>값 변경 여부</returns>
        public static bool Set<TProperty>(this INotifyPropertyChanging obj, ref TProperty target, TProperty value, PropertyChangingEventHandler propertyChangingEvent, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<TProperty>.Default.Equals(target, value))
            {
                propertyChangingEvent?.Invoke(obj, new PropertyChangingEventArgs(propertyName));
                target = value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 속성 설정
        /// </summary>
        /// <typeparam name="TObject">속성을 포함한 객체 형식. INotifyPropertyChanged, INotifyPropertyChanging를 구현해야 함.</typeparam>
        /// <typeparam name="TProperty">속성 형식</typeparam>
        /// <param name="obj">속성을 포함한 객체</param>
        /// <param name="target">속성 저장 멤버</param>
        /// <param name="value">설정 값</param>
        /// <param name="propertyChangingEvent">PropertyChanging 이벤트</param>
        /// <param name="propertyChangedEvent">PropertyChanged 이벤트</param>
        /// <param name="propertyName">속성 명</param>
        /// <returns>값 변경 여부</returns>
        public static bool Set<TObject, TProperty>(this TObject obj, ref TProperty target, TProperty value, PropertyChangingEventHandler propertyChangingEvent, PropertyChangedEventHandler propertyChangedEvent, [CallerMemberName] string propertyName = null)
            where TObject : INotifyPropertyChanged, INotifyPropertyChanging
        {
            if (!EqualityComparer<TProperty>.Default.Equals(target, value))
            {
                propertyChangingEvent?.Invoke(obj, new PropertyChangingEventArgs(propertyName));
                target = value;
                propertyChangedEvent?.Invoke(obj, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }


    }
}
