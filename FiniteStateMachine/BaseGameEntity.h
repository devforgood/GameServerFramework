#pragma once
class BaseGameEntity
{
private:
	int id_;
	static int next_vailid_id_;

	void SetID(int val);

public:
	BaseGameEntity(int id)
	{
		SetID(id);
	}

	virtual ~BaseGameEntity() {}

	virtual void Update() = 0;
	int ID() const { return id_; }
};

