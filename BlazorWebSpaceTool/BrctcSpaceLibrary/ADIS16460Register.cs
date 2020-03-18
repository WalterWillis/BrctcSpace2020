namespace BrctcSpaceLibrary
{
    public enum Register : byte
    {
        FLASH_CNT = 0x00,  //Flash memory write count
        DIAG_STAT = 0x02,  //Diagnostic and operational status
        X_GYRO_LOW = 0x04,  //X-axis gyroscope output, lower word
        X_GYRO_OUT = 0x06,  //X-axis gyroscope output, upper word
        Y_GYRO_LOW = 0x08,  //Y-axis gyroscope output, lower word
        Y_GYRO_OUT = 0x0A,  //Y-axis gyroscope output, upper word
        Z_GYRO_LOW = 0x0C,  //Z-axis gyroscope output, lower word
        Z_GYRO_OUT = 0x0E,  //Z-axis gyroscope output, upper word
        X_ACCL_LOW = 0x10,  //X-axis accelerometer output, lower word
        X_ACCL_OUT = 0x12,  //X-axis accelerometer output, upper word
        Y_ACCL_LOW = 0x14,  //Y-axis accelerometer output, lower word
        Y_ACCL_OUT = 0x16,  //Y-axis accelerometer output, upper word
        Z_ACCL_LOW = 0x18,  //Z-axis accelerometer output, lower word
        Z_ACCL_OUT = 0x1A,  //Z-axis accelerometer output, upper word
        SMPL_CNTR = 0x1C,  //Sample Counter, MSC_CTRL[3:2]=11
        TEMP_OUT = 0x1E,  //Temperature output (internal, not calibrated)
        X_DELT_ANG = 0x24,  //X-axis delta angle output
        Y_DELT_ANG = 0x26,  //Y-axis delta angle output
        Z_DELT_ANG = 0x28,  //Z-axis delta angle output
        X_DELT_VEL = 0x2A,  //X-axis delta velocity output
        Y_DELT_VEL = 0x2C,  //Y-axis delta velocity output
        Z_DELT_VEL = 0x2E,  //Z-axis delta velocity output
        MSC_CTRL = 0x32,  //Miscellaneous control
        SYNC_SCAL = 0x34,  //Sync input scale control
        DEC_RATE = 0x36,  //Decimation rate control
        FLTR_CTRL = 0x38,  //Filter control, auto-null record time
        GLOB_CMD = 0x3E,  //Global commands
        XGYRO_OFF = 0x40,  //X-axis gyroscope bias offset error
        YGYRO_OFF = 0x42,  //Y-axis gyroscope bias offset error
        ZGYRO_OFF = 0x44,  //Z-axis gyroscope bias offset factor
        XACCL_OFF = 0x46,  //X-axis acceleration bias offset factor
        YACCL_OFF = 0x48,  //Y-axis acceleration bias offset factor
        ZACCL_OFF = 0x4A,  //Z-axis acceleration bias offset factor
        LOT_ID1 = 0x52,  //Lot identification number
        LOT_ID2 = 0x54,  //Lot identification number
        PROD_ID = 0x56,  //Product identifier
        SERIAL_NUM = 0x58,  //Lot-specific serial number
        CAL_SGNTR = 0x60,  //Calibration memory signature value
        CAL_CRC = 0x62,  //Calibration memory CRC values
        CODE_SGNTR = 0x64,  //Code memory signature value
        CODE_CRC = 0x66  //Code memory CRC values
    }

}
