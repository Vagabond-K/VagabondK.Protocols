using System.ComponentModel;
using VagabondK.Protocols.Modbus.Data;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus 슬레이브
    /// </summary>
    public sealed class ModbusSlave : INotifyPropertyChanged
    {
        private ModbusBits coils = new ModbusBits();
        private ModbusBits discreteInputs = new ModbusBits();
        private ModbusWords holdingRegisters = new ModbusWords();
        private ModbusWords inputRegisters = new ModbusWords();

        /// <summary>
        /// Coils
        /// </summary>
        public ModbusBits Coils { get => coils; set => this.Set(ref coils, value, PropertyChanged); }

        /// <summary>
        /// Discrete Inputs
        /// </summary>
        public ModbusBits DiscreteInputs { get => discreteInputs; set => this.Set(ref discreteInputs, value, PropertyChanged); }

        /// <summary>
        /// Holding Registers
        /// </summary>
        public ModbusWords HoldingRegisters { get => holdingRegisters; set => this.Set(ref holdingRegisters, value, PropertyChanged); }

        /// <summary>
        /// Input Registers
        /// </summary>
        public ModbusWords InputRegisters { get => inputRegisters; set => this.Set(ref inputRegisters, value, PropertyChanged); }

        /// <summary>
        /// 속성 값이 변경될 때 발생합니다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
