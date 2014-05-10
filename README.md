pmu_sync_sampler for Nexus 7 (2012 Model)
================

A fork of https://github.com/castl/pmu_sync_sampler that adds support for collecting and experimenting on PMU samples from a Nexus 7.

##Folder Information
- *AndroidPmuReaderApp*: Contains Android-related services to read the PMU samples and send them via WiFi.
- *KernelMods*: Modifications made to the Linux Kernel that enable PMUs to be correctly read on the Nexus 7.  See _changes.txt_ in the _KernelMods_ folder for more information.
- *module*:  Kernel module that reads PMU events and puts them into a buffer.
- *reader*:  TCP server to read PMU samples and save to a file.  Contains code to parse through the raw PMU data.
- *SampleParser*:  C# applications that further perform parsing and filtering of the PMU data.  Contains code to perform aggregation of the raw data.
- *Scripts*:  Shell scripts used when collecting raw PMU samples from the Nexus 7. Other scripts used for filtering, aggregating, and classifying PMU events.
- *sender*: Unmodififed from original source.  See _AndroidPmuReaderApp\jni\PacketSender_ for files related to reading raw PMU data from a buffer.