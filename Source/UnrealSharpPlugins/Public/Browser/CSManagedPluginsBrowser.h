#pragma once

class SCSManagedPluginsBrowser : public SCompoundWidget
{
	DECLARE_DELEGATE_RetVal(FReply, FReplyDelegate);

	SLATE_BEGIN_ARGS(SCSManagedPluginsBrowser)
	{
	}
	SLATE_END_ARGS()
	
	void Construct(const FArguments& Args);
};
