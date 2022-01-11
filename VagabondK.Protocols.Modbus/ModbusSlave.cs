using System.ComponentModel;
using VagabondK.Protocols.Modbus.Data;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus 슬레이브
    /// </summary>
    public sealed class ModbusSlave : INotifyPropertyChanged
    {
        private ModbusBooleans coils = new ModbusBooleans();
        private ModbusBooleans discreteInputs = new ModbusBooleans();
        private ModbusRegisters holdingRegisters = new ModbusRegisters();
        private ModbusRegisters inputRegisters = new ModbusRegisters();

        /// <summary>
        /// Coils
        /// </summary>
        public ModbusBooleans Coils { get => coils; set => this.Set(ref coils, value, PropertyChanged); }

        /// <summary>
        /// Discrete Inputs
        /// </summary>
        public ModbusBooleans DiscreteInputs { get => discreteInputs; set => this.Set(ref discreteInputs, value, PropertyChanged); }

        /// <summary>
        /// Holding Registers
        /// </summary>
        public ModbusRegisters HoldingRegisters { get => holdingRegisters; set => this.Set(ref holdingRegisters, value, PropertyChanged); }

        /// <summary>
        /// Input Registers
        /// </summary>
        public ModbusRegisters InputRegisters { get => inputRegisters; set => this.Set(ref inputRegisters, value, PropertyChanged); }

        /// <summary>
        /// 속성 값이 변경될 때 발생합니다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
