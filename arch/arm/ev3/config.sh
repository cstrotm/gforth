echo -e "\033[35;48m" 
echo
echo
echo "Starte config.sh" 
echo
echo
echo -e "\033[0m"
skipcode=".skip 4\n.skip 4\n.skip 4\n.skip 4"
kernel_fi=kernl32l.fi
ac_cv_sizeof_void_p=4
ac_cv_sizeof_char_p=4
ac_cv_sizeof_char=1
ac_cv_sizeof_short=2
ac_cv_sizeof_int=4
ac_cv_sizeof_long=4
ac_cv_sizeof_long_long=8
ac_cv_sizeof_intptr_t=4
ac_cv_sizeof_int128_t=0
ac_cv_c_bigendian=no
ac_cv_func_memcmp_working=yes
ac_cv_func_memmove=yes
ac_cv_file___arch_arm_asm_fs=yes
ac_cv_file___arch_arm_disasm_fs=yes
ac_cv_func_dlopen=yes
ac_export_dynamic=yes
extraccdir=/data/data/gnu.gforth/lib
asm_fs=arch/arm/asm.fs
disasm_fs=arch/arm/disasm.fs
CFLAGS="-mcpu=arm926ej-s -march=armv5te"
LDLAGS="-mcpu=arm926ej-s -march=armv5te"
EC_MODE="false"
NO_EC=""
EC=""
engine2='engine2$(OPT).o'
engine_fast2='engine-fast2$(OPT).o'
no_dynamic=""
image_i=""
LIBS="-ldl"
echo "Ich wurde benutzt! " >erfolg.txt