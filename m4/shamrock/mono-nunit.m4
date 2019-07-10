AC_DEFUN([SHAMROCK_CHECK_MONO_NUNIT],
[
	PKG_CHECK_MODULES(MONO_NUNIT, mono-nunit >= 1.0, 
		do_tests="yes", do_tests="no")
	
	AC_SUBST(MONO_NUNIT_LIBS)
	AM_CONDITIONAL(ENABLE_TESTS, test "x$do_tests" = "xyes")

	if test "x$do_tests" = "xno"; then
		AC_MSG_WARN([Could not find mono-nunit: tests will not be available.])
	fi
])
