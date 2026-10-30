/* The ARM64 layout has no CRT, process state, or background worker. */
int __stdcall DllMain(void *instance, unsigned long reason, void *reserved)
{
    (void)instance;
    (void)reason;
    (void)reserved;
    return 1;
}
