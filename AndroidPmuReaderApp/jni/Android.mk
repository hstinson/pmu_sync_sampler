LOCAL_PATH := $(call my-dir)
LOCAL_ARM_MODE := arm

include $(CLEAR_VARS)
LOCAL_CFLAGS += -std=c++0x -fexceptions
LOCAL_LDLIBS := -llog
LOCAL_MODULE    := perf_counter_lib
LOCAL_SRC_FILES := perf_counter_lib.cpp eventsManager.cpp TextReader.cpp PacketSender/sender.cpp PacketSender/packet.cpp PacketSender/network.cpp PacketSender/process_info.cpp


include $(BUILD_SHARED_LIBRARY)