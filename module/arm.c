/*
 * Methods for handling PMU sampling and IRQs on a Tegra3 powered device.
 */

#include "pmu_api.h"

#include <linux/interrupt.h>
#include <linux/platform_device.h>
#include <linux/irq_work.h>
#include <linux/cpumask.h>
#include <linux/cpu.h>
#include <asm/uaccess.h>
#include <../mach-tegra/include/mach/irqs.h>
#include <asm/io.h>
#include <../include/asm/pmu.h>

#include "v7_pmu.h"

unsigned long num_ctrs = 6;

#define INT_CPU0 INT_CPU0_PMU_INTR
#define INT_CPU1 INT_CPU1_PMU_INTR
#define INT_CPU2 INT_CPU2_PMU_INTR
#define INT_CPU3 INT_CPU3_PMU_INTR

callback cpuOnlineCallback;

uint64_t read_ccnt(void) {
	return read_ccnt_int();
}

uint64_t read_pmn(unsigned i) {
	return read_pmn_int(i);
}

void dump_regs(void) {
    u32 val;
    unsigned int cnt;

    printk(KERN_INFO "PMNC Core %u registers dump:\n", smp_processor_id());

    asm volatile("mrc p15, 0, %0, c9, c12, 0" : "=r" (val));
    printk(KERN_INFO "PMNC  =0x%08x\n", val);

    asm volatile("mrc p15, 0, %0, c9, c12, 1" : "=r" (val));
    printk(KERN_INFO "CNTENS=0x%08x\n", val);

    asm volatile("mrc p15, 0, %0, c9, c14, 1" : "=r" (val));
    printk(KERN_INFO "INTENS=0x%08x\n", val);

    asm volatile("mrc p15, 0, %0, c9, c12, 3" : "=r" (val));
    printk(KERN_INFO "FLAGS =0x%08x\n", val);

    asm volatile("mrc p15, 0, %0, c9, c12, 5" : "=r" (val));
    printk(KERN_INFO "SELECT=0x%08x\n", val);

    asm volatile("mrc p15, 0, %0, c9, c13, 0" : "=r" (val));
    printk(KERN_INFO "CCNT  =0x%08x\n", val);

    asm volatile("mrc p15, 0, %0, c9, c14, 0" : "=r" (val));
    printk(KERN_INFO "USREN =0x%08x\n", val);
    
    asm volatile("mrc p14, 0, %0, c7, c14, 6" : "=r" (val));
    printk(KERN_INFO "DBGAUTHSTATUS: %x", val);

    for (cnt = 0; cnt < num_ctrs; cnt++) {
        printk(KERN_INFO "CNT[%d] count =0x%08x\n",
            cnt, read_pmn_int(cnt));
        asm volatile("mrc p15, 0, %0, c9, c13, 1" : "=r" (val));
        printk(KERN_INFO "CNT[%d] evtsel=0x%08x\n",
            cnt, val);
    }

}

// Event handler for when a PMU cycle count overflow interrupt occurs
// on one of the CPU cores.
static irqreturn_t sample_handle_irq(int irqnum, void* dev)
{  
    unsigned int flags;   
    
    if (irqnum != INT_CPU0 &&
        irqnum != INT_CPU1 &&
        irqnum != INT_CPU2 &&
        irqnum != INT_CPU3 )
    {
        printk ("IRQ number was different from expected.\n");
        return IRQ_NONE;
    }
      
    disable_pmu();
    
    //printk(KERN_INFO "IRQ on core %u\tIRQ Number is %u\n", smp_processor_id(), infoStruct->irqnum);

    flags = read_flags();
    if (flags == 0) 
    {
        printk(KERN_WARNING "Possible interrupt error on core %u. IRQ Num: %u\tFlags: 0x%x", smp_processor_id(), irqnum, flags);
        return IRQ_NONE;
    }
    
    gatherSample();
    
    reset_pmn();
    if (shutdown == 0)
    {
        write_ccnt(0xFFFFFFFF - period);
    }

    // Reset overflow flags
    write_flags(0xFFFFFFFF);
    
    if (shutdown == 0)
    {
        enable_pmu();
    }  
       
    return IRQ_HANDLED;
}

// Configures PMU cycle count interrupts for all CPU cores.
static int config_irq(void) 
{
    unsigned long irq_flags;
    int rc;   
    
    irq_flags = IRQF_NOBALANCING;

    rc = request_irq(INT_CPU0, sample_handle_irq, irq_flags,
                    "pmu_sync0", NULL);
    if (rc)
    {
        return rc;
    }
  
    rc = request_irq(INT_CPU1, sample_handle_irq, irq_flags,
                    "pmu_sync1", NULL);
    if (rc) 
    {
        free_irq(INT_CPU0, NULL);
        return rc;
    }
    
    rc = request_irq(INT_CPU2, sample_handle_irq, irq_flags,
                    "pmu_sync2", NULL);
    if (rc) 
    {
        free_irq(INT_CPU0, NULL);
        free_irq(INT_CPU1, NULL);
        return rc;
    }
    
    rc = request_irq(INT_CPU3, sample_handle_irq, irq_flags,
                    "pmu_sync3", NULL);
    if (rc) 
    {
        free_irq(INT_CPU0, NULL);
        free_irq(INT_CPU1, NULL);
        free_irq(INT_CPU2, NULL);
        return rc;
    }

    return 0;
}

// CPU Online / Offline Handlers
// These methods enabled /disable PMU sample gathering for a CPU
// based on if the CPU is online or offline
// Reference: https://www.kernel.org/doc/Documentation/cpu-hotplug.txt

void registerCpuOnlineCallback(callback ptrCallback)
{
    cpuOnlineCallback = ptrCallback;
}

void onCpuOnline(void *info)
{
    printk(KERN_INFO "PMU Sampler: CPU online\n");  
    startCtrsLocal();  
    //if (cpuOnlineCallback != NULL)
    //{
    //    printk(KERN_INFO "Firing Callback!!!!"); 
    //    (*cpuOnlineCallback)(NULL);
    //}
}

void onCpuOffline(void *info)
{
    printk(KERN_INFO "PMU Sampler: CPU offline\n");  
    stopCtrsLocal(NULL);
}

// Called when a CPU comes online / goes offline
int cpu_callback(struct notifier_block *nfb,
		 unsigned long action, void *hcpu)
{
    unsigned int cpu = (unsigned long)hcpu;
    printk(KERN_INFO "CPU Change on CPU %u\n", cpu); 
    // Wait until PMU values have been set.  Otherwise the module will crash.
    if (shutdown == 0 && eventConfigs[0] > 0) 
    {
        switch (action) 
        {
            case CPU_ONLINE:
            case CPU_ONLINE_FROZEN:
                printk(KERN_INFO "CPU %u Online \n", cpu);
                smp_call_function_single(cpu, onCpuOnline, NULL, 1);
                break;
            case CPU_DOWN_PREPARE:
            case CPU_DOWN_PREPARE_FROZEN:
                printk(KERN_INFO "CPU %u Going Down \n", cpu);
                smp_call_function_single(cpu, onCpuOffline, NULL, 1);
                break;
        }     
    }       
    
    return NOTIFY_OK;
}

static struct notifier_block cpu_notifier =
{
   .notifier_call = cpu_callback,
};

// No-ops
void register_interrupt(void) { }
void deregister_interrupt(void) { }

void stopCtrsLocal(void* d) 
{
    unsigned int i;
    unsigned int proc = smp_processor_id();
    
    disable_ccnt_irq();
    disable_ccnt();
    for (i = 0; i < num_ctrs; i++) 
    {
        disable_pmn(i); 
    }
    disable_pmu();
    
    printk(KERN_INFO "Disabled PMU on core %u\n", proc);   
}

void startCtrsLocal(void)
{   
    unsigned int i;
    unsigned int proc = smp_processor_id();
    printk(KERN_INFO "PMU Sampler: Configuring PMU on core %u\n", proc);       

    if (init_pmu_single(proc))
    {
        printk(KERN_ERR "Unable to set IRQ affinity for PMU interrupt for CPU %u!\n", proc);
    }
    
    disable_pmu();
    disable_ccnt();

    reset_ccnt();
    reset_pmn();

    // Reset overflow flags
    write_flags(0xFFFFFFFF);

    // Overflow once every 'period' cycles
    write_ccnt(0xFFFFFFFF - period);
    for (i=0; i<num_ctrs; i++) {
        pmn_config(i, eventConfigs[i]); 
    }

    dump_regs();

    enable_ccnt_irq();
    for (i = 0; i < num_ctrs; i++) 
    {
        enable_pmn(i);
    }

    enable_ccnt();
    enable_pmu();
}

int initialize_arch(void) 
{
    int rc;
    rc = 0;
    
    if (rc)
    {
        printk(KERN_ERR "Unable to set IRQ affinity for PMU interrupts!\n");
        return -1;
    }
    
    num_ctrs = getPMN();

    printk(KERN_INFO "    Found %lu counters", num_ctrs);
    printk(KERN_INFO "    Configuring interrupt handler");

    rc = config_irq();
    if (rc) 
    {
        printk(KERN_ERR "    -> Error configuring interrupts (%d)!!!", rc);
        return -1;
    }  
    
    printk(KERN_INFO "    Registering CPU notifier\n");
    register_hotcpu_notifier(&cpu_notifier);

    return 0;
}

void cleanup_arch(void) 
{
    unregister_hotcpu_notifier(&cpu_notifier);
    free_irq(INT_CPU0, NULL);
    free_irq(INT_CPU1, NULL);
    free_irq(INT_CPU2, NULL);
    free_irq(INT_CPU3, NULL);
}

